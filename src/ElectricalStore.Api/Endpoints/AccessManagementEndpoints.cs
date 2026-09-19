using Permixa.Application.Audit.Get;
using Permixa.Application.Authorization;
using Permixa.Application.Authorization.Hierarchy;
using Permixa.Application.Authorization.Permissions.Get;
using Permixa.Application.Authorization.Permissions.Models;
using Permixa.Application.Authorization.RolePermissions.Assign;
using Permixa.Application.Authorization.RolePermissions.Get;
using Permixa.Application.Authorization.RolePermissions.Remove;
using Permixa.Application.Authorization.Roles.ChangePosition;
using Permixa.Application.Authorization.Roles.Create;
using Permixa.Application.Authorization.Roles.Delete;
using Permixa.Application.Authorization.Roles.Get;
using Permixa.Application.Authorization.Roles.Models;
using Permixa.Application.Authorization.Roles.Rename;
using Permixa.Application.Authorization.UserPermissionOverrides.Get;
using Permixa.Application.Authorization.UserPermissionOverrides.Remove;
using Permixa.Application.Authorization.UserPermissionOverrides.Set;
using Permixa.Application.Authorization.UserRoles.Assign;
using Permixa.Application.Authorization.UserRoles.Get;
using Permixa.Application.Authorization.UserRoles.Remove;
using Permixa.Application.Authorization.Users.Get;
using Permixa.Application.Authorization.Users.Models;
using Permixa.Application.Common.Paging;
using Permixa.AspNetCore.Authorization;
using Permixa.AspNetCore.Http;
using Permixa.AspNetCore.Security;
using Permixa.Domain.Authorization;

namespace ElectricalStore.Api.Endpoints;

/// <summary>
/// Thin Back Office adapters over Permixa IAM use cases. No host-owned IAM tables.
/// </summary>
public static class AccessManagementEndpoints
{
    public const string Tag = "Back Office - Access Management";

    public static IEndpointRouteBuilder MapAccessManagementEndpoints(this IEndpointRouteBuilder app)
    {
        var access = app.MapGroup("/admin/access")
            .WithTags(Tag)
            .RequireAuthorization();

        MapUsers(access);
        MapRoles(access);
        MapPermissions(access);
        MapAudit(access);

        return app;
    }

    private static void MapUsers(RouteGroupBuilder access)
    {
        access.MapGet("/users", async (
                ICurrentUser currentUser,
                GetUsersUseCase useCase,
                HttpContext http,
                int? page,
                int? pageSize,
                string? search,
                bool? isDisabled,
                bool? isLocked,
                CancellationToken cancellationToken) =>
            {
                if (!TryActor(currentUser, out var actorId, out var unauthorized))
                    return unauthorized;

                var result = await useCase.ExecuteAsync(
                    new GetUsersQuery(
                        actorId,
                        new PageRequest(page ?? 1, pageSize ?? 20),
                        search,
                        isDisabled,
                        isLocked),
                    cancellationToken);

                return result.ToHttpResult(http, value => Results.Json(value));
            })
            .RequirePermission(IamPermissions.Users.Read)
            .WithName("ListAccessUsers")
            .WithSummary("List users (IAM)");

        access.MapGet("/users/{id:guid}", async (
                Guid id,
                ICurrentUser currentUser,
                GetUserIamDetailsUseCase useCase,
                GetPermissionsUseCase permissions,
                HttpContext http,
                CancellationToken cancellationToken) =>
            {
                if (!TryActor(currentUser, out var actorId, out var unauthorized))
                    return unauthorized;

                var details = await useCase.ExecuteAsync(
                    new GetUserIamDetailsQuery(actorId, id),
                    cancellationToken);
                if (!details.IsSuccess)
                    return details.ToHttpResult(http, _ => Results.Ok());

                var catalog = await permissions.ExecuteAsync(
                    new GetPermissionsQuery(actorId),
                    cancellationToken);
                if (!catalog.IsSuccess)
                    return catalog.ToHttpResult(http, _ => Results.Ok());

                return Results.Json(ToUserAccessDetails(details.Value!, catalog.Value!));
            })
            .RequirePermission(IamPermissions.Users.Read)
            .WithName("GetAccessUser")
            .WithSummary("Get user IAM details including effective permissions");

        access.MapPut("/users/{id:guid}/roles", async (
                Guid id,
                SetUserRolesRequest request,
                ICurrentUser currentUser,
                GetUserRolesUseCase getRoles,
                AssignRoleToUserUseCase assign,
                RemoveRoleFromUserUseCase remove,
                HttpContext http,
                CancellationToken cancellationToken) =>
            {
                if (!TryActor(currentUser, out var actorId, out var unauthorized))
                    return unauthorized;

                var desired = (request.RoleIds ?? Array.Empty<Guid>()).Distinct().ToArray();
                var currentResult = await getRoles.ExecuteAsync(
                    new GetUserRolesQuery(actorId, id),
                    cancellationToken);
                if (!currentResult.IsSuccess)
                    return currentResult.ToHttpResult(http, _ => Results.Ok());

                var currentIds = currentResult.Value!.Select(r => r.Id).ToHashSet();
                var desiredIds = desired.ToHashSet();

                foreach (var roleId in desiredIds.Except(currentIds))
                {
                    var assignResult = await assign.ExecuteAsync(
                        new AssignRoleToUserCommand(actorId, id, roleId),
                        cancellationToken);
                    if (!assignResult.IsSuccess)
                        return assignResult.ToHttpResult(http, () => Results.Ok());
                }

                foreach (var roleId in currentIds.Except(desiredIds))
                {
                    var removeResult = await remove.ExecuteAsync(
                        new RemoveRoleFromUserCommand(actorId, id, roleId),
                        cancellationToken);
                    if (!removeResult.IsSuccess)
                        return removeResult.ToHttpResult(http, () => Results.Ok());
                }

                var refreshed = await getRoles.ExecuteAsync(
                    new GetUserRolesQuery(actorId, id),
                    cancellationToken);
                return refreshed.ToHttpResult(http, value => Results.Json(value));
            })
            .RequirePermission(IamPermissions.UserRoles.Manage)
            .WithName("SetAccessUserRoles")
            .WithSummary("Replace user role assignments");

        access.MapGet("/users/{id:guid}/permissions", async (
                Guid id,
                ICurrentUser currentUser,
                GetUserIamDetailsUseCase useCase,
                GetPermissionsUseCase permissions,
                HttpContext http,
                CancellationToken cancellationToken) =>
            {
                if (!TryActor(currentUser, out var actorId, out var unauthorized))
                    return unauthorized;

                var details = await useCase.ExecuteAsync(
                    new GetUserIamDetailsQuery(actorId, id),
                    cancellationToken);
                if (!details.IsSuccess)
                    return details.ToHttpResult(http, _ => Results.Ok());

                var catalog = await permissions.ExecuteAsync(
                    new GetPermissionsQuery(actorId),
                    cancellationToken);
                if (!catalog.IsSuccess)
                    return catalog.ToHttpResult(http, _ => Results.Ok());

                var view = ToUserAccessDetails(details.Value!, catalog.Value!);
                return Results.Json(new
                {
                    userId = view.User.Id,
                    roles = view.Roles,
                    permissions = view.Permissions
                });
            })
            .RequirePermission(IamPermissions.Users.Read)
            .WithName("GetAccessUserEffectivePermissions")
            .WithSummary("Get user effective permissions with override provenance");

        access.MapPut("/users/{id:guid}/permissions/{permissionId:guid}", async (
                Guid id,
                Guid permissionId,
                SetUserPermissionOverrideRequest request,
                ICurrentUser currentUser,
                SetUserPermissionOverrideUseCase useCase,
                HttpContext http,
                CancellationToken cancellationToken) =>
            {
                if (!TryActor(currentUser, out var actorId, out var unauthorized))
                    return unauthorized;

                if (!TryParseEffect(request.Effect, out var effect, out var badRequest))
                    return badRequest;

                var result = await useCase.ExecuteAsync(
                    new SetUserPermissionOverrideCommand(actorId, id, permissionId, effect),
                    cancellationToken);
                return result.ToHttpResult(http, value => Results.Json(value));
            })
            .RequirePermission(IamPermissions.UserPermissionOverrides.Manage)
            .WithName("SetAccessUserPermissionOverride")
            .WithSummary("Allow or deny a permission override for a user");

        access.MapDelete("/users/{id:guid}/permissions/{permissionId:guid}", async (
                Guid id,
                Guid permissionId,
                ICurrentUser currentUser,
                RemoveUserPermissionOverrideUseCase useCase,
                HttpContext http,
                CancellationToken cancellationToken) =>
            {
                if (!TryActor(currentUser, out var actorId, out var unauthorized))
                    return unauthorized;

                var result = await useCase.ExecuteAsync(
                    new RemoveUserPermissionOverrideCommand(actorId, id, permissionId),
                    cancellationToken);
                return result.ToHttpResult(http, () => Results.NoContent());
            })
            .RequirePermission(IamPermissions.UserPermissionOverrides.Manage)
            .WithName("RemoveAccessUserPermissionOverride")
            .WithSummary("Remove user permission override (inherit again)");
    }

    private static void MapRoles(RouteGroupBuilder access)
    {
        access.MapGet("/roles", async (
                ICurrentUser currentUser,
                GetRolesUseCase useCase,
                GetMyRolesUseCase myRoles,
                HttpContext http,
                string? search,
                CancellationToken cancellationToken) =>
            {
                if (!TryActor(currentUser, out var actorId, out var unauthorized))
                    return unauthorized;

                var result = await useCase.ExecuteAsync(new GetRolesQuery(actorId), cancellationToken);
                if (!result.IsSuccess)
                    return result.ToHttpResult(http, _ => Results.Ok());

                // GetRoles returns only hierarchically manageable roles (strictly weaker).
                // Union actor's own roles (GetMyRoles) so Dashboard can see Owner as a create reference.
                var mine = await myRoles.ExecuteAsync(new GetMyRolesQuery(actorId), cancellationToken);
                if (!mine.IsSuccess)
                    return mine.ToHttpResult(http, _ => Results.Ok());

                IEnumerable<RoleDto> items = result.Value!
                    .Concat(mine.Value!)
                    .GroupBy(r => r.Id)
                    .Select(g => g.First());

                if (!string.IsNullOrWhiteSpace(search))
                {
                    items = items.Where(r =>
                        r.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                return Results.Json(items.OrderBy(r => r.RoleLevel).ThenBy(r => r.Name).ToArray());
            })
            .RequirePermission(IamPermissions.Roles.Read)
            .WithName("ListAccessRoles")
            .WithSummary("List roles with RoleLevel (manageable + actor's own)");

        access.MapGet("/me/roles", async (
                ICurrentUser currentUser,
                GetMyRolesUseCase useCase,
                HttpContext http,
                CancellationToken cancellationToken) =>
            {
                if (!TryActor(currentUser, out var actorId, out var unauthorized))
                    return unauthorized;

                var result = await useCase.ExecuteAsync(new GetMyRolesQuery(actorId), cancellationToken);
                return result.ToHttpResult(http, value => Results.Json(value));
            })
            .WithName("GetAccessMyRoles")
            .WithSummary("Get the current actor's assigned roles (hierarchy reference)");


        access.MapGet("/roles/{id:guid}", async (
                Guid id,
                ICurrentUser currentUser,
                GetRoleByIdUseCase getRole,
                GetRolePermissionsUseCase getPermissions,
                HttpContext http,
                CancellationToken cancellationToken) =>
            {
                if (!TryActor(currentUser, out var actorId, out var unauthorized))
                    return unauthorized;

                var roleResult = await getRole.ExecuteAsync(new GetRoleByIdQuery(actorId, id), cancellationToken);
                if (!roleResult.IsSuccess)
                    return roleResult.ToHttpResult(http, _ => Results.Ok());

                var permResult = await getPermissions.ExecuteAsync(
                    new GetRolePermissionsQuery(actorId, id),
                    cancellationToken);
                if (!permResult.IsSuccess)
                    return permResult.ToHttpResult(http, _ => Results.Ok());

                return Results.Json(new
                {
                    role = roleResult.Value,
                    permissions = permResult.Value
                });
            })
            .RequirePermission(IamPermissions.Roles.Read)
            .WithName("GetAccessRole")
            .WithSummary("Get role details and granted permissions");

        access.MapPost("/roles", async (
                CreateAccessRoleRequest request,
                ICurrentUser currentUser,
                CreateRoleUseCase useCase,
                HttpContext http,
                CancellationToken cancellationToken) =>
            {
                if (!TryActor(currentUser, out var actorId, out var unauthorized))
                    return unauthorized;

                if (!TryParsePlacement(request.Placement, out var placement, out var badRequest))
                    return badRequest;

                var result = await useCase.ExecuteAsync(
                    new CreateRoleCommand(actorId, request.Name, request.ReferenceRoleId, placement),
                    cancellationToken);
                return result.ToHttpResult(http, value =>
                    Results.Json(value, statusCode: StatusCodes.Status201Created));
            })
            .RequirePermission(IamPermissions.Roles.Create)
            .WithName("CreateAccessRole")
            .WithSummary("Create role relative to a reference role (RolePlacement)");

        access.MapPut("/roles/{id:guid}", async (
                Guid id,
                UpdateAccessRoleRequest request,
                ICurrentUser currentUser,
                RenameRoleUseCase rename,
                ChangeRolePositionUseCase changePosition,
                GetRoleByIdUseCase getRole,
                HttpContext http,
                CancellationToken cancellationToken) =>
            {
                if (!TryActor(currentUser, out var actorId, out var unauthorized))
                    return unauthorized;

                var hasName = !string.IsNullOrWhiteSpace(request.Name);
                var hasPosition = request.ReferenceRoleId.HasValue || !string.IsNullOrWhiteSpace(request.Placement);
                if (!hasName && !hasPosition)
                {
                    return Results.Problem(
                        detail: "Provide Name and/or ReferenceRoleId+Placement.",
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Authorization.InvalidRoleUpdate",
                        extensions: new Dictionary<string, object?> { ["code"] = "Authorization.InvalidRoleUpdate" });
                }

                if (hasName)
                {
                    var renameResult = await rename.ExecuteAsync(
                        new RenameRoleCommand(actorId, id, request.Name!),
                        cancellationToken);
                    if (!renameResult.IsSuccess)
                        return renameResult.ToHttpResult(http, value => Results.Json(value));
                }

                if (hasPosition)
                {
                    if (!request.ReferenceRoleId.HasValue || string.IsNullOrWhiteSpace(request.Placement))
                    {
                        return Results.Problem(
                            detail: "ReferenceRoleId and Placement are required together.",
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Authorization.InvalidRolePlacement",
                            extensions: new Dictionary<string, object?> { ["code"] = "Authorization.InvalidRolePlacement" });
                    }

                    if (!TryParsePlacement(request.Placement, out var placement, out var badRequest))
                        return badRequest;

                    var moveResult = await changePosition.ExecuteAsync(
                        new ChangeRolePositionCommand(actorId, id, request.ReferenceRoleId.Value, placement),
                        cancellationToken);
                    if (!moveResult.IsSuccess)
                        return moveResult.ToHttpResult(http, value => Results.Json(value));
                }

                var refreshed = await getRole.ExecuteAsync(new GetRoleByIdQuery(actorId, id), cancellationToken);
                return refreshed.ToHttpResult(http, value => Results.Json(value));
            })
            .RequirePermission(IamPermissions.Roles.Update)
            .WithName("UpdateAccessRole")
            .WithSummary("Rename role and/or change RoleLevel via placement");

        access.MapPut("/roles/{id:guid}/permissions", async (
                Guid id,
                SetRolePermissionsRequest request,
                ICurrentUser currentUser,
                GetRolePermissionsUseCase getPermissions,
                AssignPermissionToRoleUseCase assign,
                RemovePermissionFromRoleUseCase remove,
                HttpContext http,
                CancellationToken cancellationToken) =>
            {
                if (!TryActor(currentUser, out var actorId, out var unauthorized))
                    return unauthorized;

                var desired = (request.PermissionIds ?? Array.Empty<Guid>()).Distinct().ToArray();
                var currentResult = await getPermissions.ExecuteAsync(
                    new GetRolePermissionsQuery(actorId, id),
                    cancellationToken);
                if (!currentResult.IsSuccess)
                    return currentResult.ToHttpResult(http, _ => Results.Ok());

                var currentIds = currentResult.Value!.Select(p => p.Id).ToHashSet();
                var desiredIds = desired.ToHashSet();

                foreach (var permissionId in desiredIds.Except(currentIds))
                {
                    var assignResult = await assign.ExecuteAsync(
                        new AssignPermissionToRoleCommand(actorId, id, permissionId),
                        cancellationToken);
                    if (!assignResult.IsSuccess)
                        return assignResult.ToHttpResult(http, () => Results.Ok());
                }

                foreach (var permissionId in currentIds.Except(desiredIds))
                {
                    var removeResult = await remove.ExecuteAsync(
                        new RemovePermissionFromRoleCommand(actorId, id, permissionId),
                        cancellationToken);
                    if (!removeResult.IsSuccess)
                        return removeResult.ToHttpResult(http, () => Results.Ok());
                }

                var refreshed = await getPermissions.ExecuteAsync(
                    new GetRolePermissionsQuery(actorId, id),
                    cancellationToken);
                return refreshed.ToHttpResult(http, value => Results.Json(value));
            })
            .RequirePermission(IamPermissions.RolePermissions.Manage)
            .WithName("SetAccessRolePermissions")
            .WithSummary("Replace role permission grants");

        access.MapDelete("/roles/{id:guid}", async (
                Guid id,
                ICurrentUser currentUser,
                DeleteRoleUseCase useCase,
                HttpContext http,
                CancellationToken cancellationToken) =>
            {
                if (!TryActor(currentUser, out var actorId, out var unauthorized))
                    return unauthorized;

                var result = await useCase.ExecuteAsync(
                    new DeleteRoleCommand(actorId, id),
                    cancellationToken);
                return result.ToHttpResult(http, () => Results.NoContent());
            })
            .RequirePermission(IamPermissions.Roles.Delete)
            .WithName("DeleteAccessRole")
            .WithSummary("Delete role when Permixa allows (no users/permissions; Owner protected)");
    }

    private static void MapPermissions(RouteGroupBuilder access)
    {
        access.MapGet("/permissions", async (
                ICurrentUser currentUser,
                GetPermissionsUseCase useCase,
                HttpContext http,
                string? search,
                CancellationToken cancellationToken) =>
            {
                if (!TryActor(currentUser, out var actorId, out var unauthorized))
                    return unauthorized;

                var result = await useCase.ExecuteAsync(new GetPermissionsQuery(actorId), cancellationToken);
                return result.ToHttpResult(http, value =>
                {
                    IEnumerable<PermissionDto> items = value;
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        items = items.Where(p =>
                            p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                            (p.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
                    }

                    // Group by prefix before first '.' when present (metadata-friendly presentation).
                    var grouped = items
                        .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                        .GroupBy(p =>
                        {
                            var dot = p.Name.IndexOf('.', StringComparison.Ordinal);
                            return dot > 0 ? p.Name[..dot] : p.Name;
                        })
                        .Select(g => new
                        {
                            group = g.Key,
                            permissions = g.Select(p => new
                            {
                                p.Id,
                                code = p.Name,
                                p.Description
                            }).ToArray()
                        })
                        .ToArray();

                    return Results.Json(new { groups = grouped });
                });
            })
            .RequirePermission(IamPermissions.Permissions.Read)
            .WithName("ListAccessPermissions")
            .WithSummary("List registered permissions (grouped by name prefix)");
    }

    private static void MapAudit(RouteGroupBuilder access)
    {
        access.MapGet("/audit", async (
                ICurrentUser currentUser,
                GetIamAuditLogsUseCase useCase,
                HttpContext http,
                int? page,
                int? pageSize,
                Guid? actorUserId,
                Guid? targetUserId,
                string? eventType,
                CancellationToken cancellationToken) =>
            {
                if (!TryActor(currentUser, out var actorId, out var unauthorized))
                    return unauthorized;

                var result = await useCase.ExecuteAsync(
                    new GetIamAuditLogsQuery(
                        actorId,
                        new PageRequest(page ?? 1, pageSize ?? 20),
                        actorUserId,
                        targetUserId,
                        eventType,
                        Outcome: null,
                        FromUtc: null,
                        ToUtc: null),
                    cancellationToken);

                return result.ToHttpResult(http, value => Results.Json(value));
            })
            .RequirePermission(IamPermissions.Audit.Read)
            .WithName("ListAccessAuditLogs")
            .WithSummary("List Permixa IAM audit logs");
    }

    private static bool TryActor(ICurrentUser currentUser, out Guid actorId, out IResult unauthorized)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            actorId = default;
            unauthorized = Results.Unauthorized();
            return false;
        }

        actorId = currentUser.UserId.Value;
        unauthorized = Results.Empty;
        return true;
    }

    private static bool TryParseEffect(string? raw, out PermissionEffect effect, out IResult badRequest)
    {
        effect = default;
        badRequest = Results.Empty;

        if (string.IsNullOrWhiteSpace(raw))
        {
            badRequest = Results.Problem(
                detail: "Effect must be Allow or Deny.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Authorization.InvalidOverrideEffect",
                extensions: new Dictionary<string, object?> { ["code"] = "Authorization.InvalidOverrideEffect" });
            return false;
        }

        if (Enum.TryParse(raw, ignoreCase: true, out effect) &&
            (effect == PermissionEffect.Allow || effect == PermissionEffect.Deny))
        {
            return true;
        }

        badRequest = Results.Problem(
            detail: "Effect must be Allow or Deny.",
            statusCode: StatusCodes.Status400BadRequest,
            title: "Authorization.InvalidOverrideEffect",
            extensions: new Dictionary<string, object?> { ["code"] = "Authorization.InvalidOverrideEffect" });
        return false;
    }

    private static bool TryParsePlacement(string? raw, out RolePlacement placement, out IResult badRequest)
    {
        placement = default;
        badRequest = Results.Empty;

        if (string.IsNullOrWhiteSpace(raw) ||
            !Enum.TryParse(raw, ignoreCase: true, out placement) ||
            !Enum.IsDefined(placement))
        {
            badRequest = Results.Problem(
                detail: "Placement must be Above, Below, or SameLevel.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Authorization.InvalidRolePlacement",
                extensions: new Dictionary<string, object?> { ["code"] = "Authorization.InvalidRolePlacement" });
            return false;
        }

        return true;
    }

    /// <summary>
    /// Provenance is derived only from Permixa public DTOs:
    /// user overrides (Allow/Deny) + effective permission names (role grants after precedence).
    /// Precedence: UserDeny &gt; UserAllow &gt; Role &gt; DefaultDeny.
    /// </summary>
    private static UserAccessDetailsResponse ToUserAccessDetails(
        UserIamDetailsDto details,
        IReadOnlyList<PermissionDto> catalog)
    {
        var effective = details.EffectivePermissions.ToHashSet(StringComparer.Ordinal);
        var overridesByName = details.Overrides
            .ToDictionary(o => o.PermissionName, o => o, StringComparer.Ordinal);

        var permissions = catalog
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Select(p =>
            {
                if (overridesByName.TryGetValue(p.Name, out var ov))
                {
                    var isAllow = ov.Effect == PermissionEffect.Allow;
                    return new EffectivePermissionItem(
                        p.Name,
                        isAllow,
                        isAllow ? "UserAllow" : "UserDeny");
                }

                if (effective.Contains(p.Name))
                    return new EffectivePermissionItem(p.Name, true, "Role");

                return new EffectivePermissionItem(p.Name, false, "DefaultDeny");
            })
            .ToArray();

        return new UserAccessDetailsResponse(
            details.User,
            details.Roles,
            details.Overrides,
            permissions);
    }

    public sealed record SetUserRolesRequest(Guid[]? RoleIds);
    public sealed record SetUserPermissionOverrideRequest(string Effect);
    public sealed record CreateAccessRoleRequest(string Name, Guid ReferenceRoleId, string Placement);
    public sealed record UpdateAccessRoleRequest(string? Name, Guid? ReferenceRoleId, string? Placement);
    public sealed record SetRolePermissionsRequest(Guid[]? PermissionIds);

    public sealed record EffectivePermissionItem(string Code, bool Effective, string Source);

    public sealed record UserAccessDetailsResponse(
        IamUserDto User,
        IReadOnlyList<RoleDto> Roles,
        IReadOnlyList<UserPermissionOverrideListItemDto> Overrides,
        IReadOnlyList<EffectivePermissionItem> Permissions);
}
