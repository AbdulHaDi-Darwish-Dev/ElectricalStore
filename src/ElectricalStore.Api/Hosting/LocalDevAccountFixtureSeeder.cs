using ElectricalStore.Application.Authorization;
using ElectricalStore.Application.Customers;
using Microsoft.Extensions.Options;
using Permixa.Application.Authentication.Models;
using Permixa.Application.Authentication.Register;
using Permixa.Application.Authorization.Abstractions;
using Permixa.Application.Common.Abstractions;
using Permixa.Application.Identity.Abstractions;
using Permixa.Application.Verification.Abstractions;
using Permixa.Domain.Authorization;

namespace ElectricalStore.Api.Hosting;

/// <summary>
/// Development-only E2E users and roles (privileged Identity writers + role grants).
/// Never registered when <c>LocalDevFixtures:Enabled</c> is false (Production baseline).
/// </summary>
public sealed class LocalDevAccountFixtureSeeder
{
    public const string AdminRoleName = "E2E-Admin";
    public const string LimitedRoleName = "E2E-Limited";

    /// <summary>Below Owner (RoleLevel 1). Deterministic levels for local fixtures only.</summary>
    private const int AdminRoleLevel = 50;
    private const int LimitedRoleLevel = 60;

    private static readonly string[] AdminPermissionNames =
    [
        AppPermissions.Categories.Manage,
        AppPermissions.Products.Manage,
        AppPermissions.Inventory.Read,
        AppPermissions.Inventory.Adjust,
        AppPermissions.Shipping.Manage,
        AppPermissions.Orders.Read,
        AppPermissions.Orders.Manage,
        AppPermissions.Settings.Manage,
        "Iam.Users.Read",
        "Iam.Roles.Read",
        "Iam.Roles.Create",
        "Iam.Roles.Update",
        "Iam.Roles.Delete",
        "Iam.RolePermissions.Manage",
        "Iam.UserRoles.Manage",
        "Iam.UserPermissionOverrides.Manage",
        "Iam.Permissions.Read",
        "Iam.Audit.Read",
    ];

    private static readonly string[] LimitedPermissionNames =
    [
        AppPermissions.Orders.Read,
        AppPermissions.Inventory.Read,
    ];

    private readonly LocalDevFixtureOptions _options;
    private readonly RegisterUserUseCase _register;
    private readonly CreateCustomerProfileForUserUseCase _createProfile;
    private readonly IIdentityUserReader _users;
    private readonly IIdentityEmailConfirmation _emailConfirmation;
    private readonly IIdentityRoleReader _roleReader;
    private readonly IIdentityRoleWriter _roleWriter;
    private readonly IIdentityUserRoleWriter _userRoleWriter;
    private readonly IPermissionRepository _permissions;
    private readonly IRolePermissionRepository _rolePermissions;
    private readonly IAuthorizationStateRepository _authorizationStates;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<LocalDevAccountFixtureSeeder> _logger;

    public LocalDevAccountFixtureSeeder(
        IOptions<LocalDevFixtureOptions> options,
        RegisterUserUseCase register,
        CreateCustomerProfileForUserUseCase createProfile,
        IIdentityUserReader users,
        IIdentityEmailConfirmation emailConfirmation,
        IIdentityRoleWriter roleWriter,
        IIdentityRoleReader roleReader,
        IIdentityUserRoleWriter userRoleWriter,
        IPermissionRepository permissions,
        IRolePermissionRepository rolePermissions,
        IAuthorizationStateRepository authorizationStates,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<LocalDevAccountFixtureSeeder> logger)
    {
        _options = options.Value;
        _register = register;
        _createProfile = createProfile;
        _users = users;
        _emailConfirmation = emailConfirmation;
        _roleReader = roleReader;
        _roleWriter = roleWriter;
        _userRoleWriter = userRoleWriter;
        _permissions = permissions;
        _rolePermissions = rolePermissions;
        _authorizationStates = authorizationStates;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return;

        if (!HasAccountCredentials())
        {
            _logger.LogInformation(
                "LocalDevFixtures accounts skipped — set LocalDevFixtures:* Email/UserName/Password via " +
                "scripts/prepare-local-e2e.ps1 (user-secrets). Catalog fixtures still apply.");
            return;
        }

        var rbacChanged = false;

        var adminRoleId = await EnsureRoleAsync(AdminRoleName, AdminRoleLevel, cancellationToken);
        var limitedRoleId = await EnsureRoleAsync(LimitedRoleName, LimitedRoleLevel, cancellationToken);

        rbacChanged |= await EnsureRolePermissionsAsync(adminRoleId, AdminPermissionNames, cancellationToken);
        rbacChanged |= await EnsureRolePermissionsAsync(limitedRoleId, LimitedPermissionNames, cancellationToken);

        await EnsureUserWithRoleAsync(
            _options.CustomerUserName,
            _options.CustomerEmail,
            _options.CustomerPassword,
            roleId: null,
            cancellationToken);

        await EnsureCustomerProfileAsync(_options.CustomerEmail, "E2E Customer", cancellationToken);

        await EnsureUserWithRoleAsync(
            _options.AdminUserName,
            _options.AdminEmail,
            _options.AdminPassword,
            adminRoleId,
            cancellationToken);

        await EnsureUserWithRoleAsync(
            _options.LimitedUserName,
            _options.LimitedEmail,
            _options.LimitedPassword,
            limitedRoleId,
            cancellationToken);

        if (rbacChanged)
        {
            var state = await _authorizationStates.GetAsync(cancellationToken)
                ?? throw new InvalidOperationException(
                    "AuthorizationState missing after LocalDevFixtures role grants.");
            state.IncrementRbacVersion();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "LocalDevFixtures accounts ready ({AdminRole}, {LimitedRole}).",
            AdminRoleName,
            LimitedRoleName);
    }

    private bool HasAccountCredentials() =>
        !string.IsNullOrWhiteSpace(_options.CustomerEmail)
        && !string.IsNullOrWhiteSpace(_options.CustomerUserName)
        && !string.IsNullOrWhiteSpace(_options.CustomerPassword)
        && !string.IsNullOrWhiteSpace(_options.AdminEmail)
        && !string.IsNullOrWhiteSpace(_options.AdminUserName)
        && !string.IsNullOrWhiteSpace(_options.AdminPassword)
        && !string.IsNullOrWhiteSpace(_options.LimitedEmail)
        && !string.IsNullOrWhiteSpace(_options.LimitedUserName)
        && !string.IsNullOrWhiteSpace(_options.LimitedPassword);

    private async Task<Guid> EnsureRoleAsync(
        string roleName,
        int roleLevel,
        CancellationToken cancellationToken)
    {
        var existing = await _roleReader.GetByNameAsync(roleName, cancellationToken);
        if (existing is not null)
            return existing.Id;

        var created = await _roleWriter.CreateAsync(roleName, roleLevel, cancellationToken);
        if (!created.Succeeded || created.RoleId is null)
        {
            throw new InvalidOperationException(
                $"LocalDevFixtures failed to create role '{roleName}' ({created.Failure}).");
        }

        _logger.LogInformation(
            "LocalDevFixtures created role {RoleName} at RoleLevel {RoleLevel}.",
            roleName,
            roleLevel);
        return created.RoleId.Value;
    }

    private async Task<bool> EnsureRolePermissionsAsync(
        Guid roleId,
        IReadOnlyList<string> permissionNames,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var added = false;
        foreach (var name in permissionNames)
        {
            var permission = await _permissions.GetByNameAsync(name, cancellationToken);
            if (permission is null)
            {
                _logger.LogWarning("LocalDevFixtures: permission '{Name}' not found; skip grant.", name);
                continue;
            }

            if (await _rolePermissions.ExistsAsync(roleId, permission.Id, cancellationToken))
                continue;

            await _rolePermissions.AddAsync(
                RolePermission.Create(roleId, permission.Id, now),
                cancellationToken);
            added = true;
        }

        return added;
    }

    private async Task EnsureCustomerProfileAsync(
        string email,
        string fullName,
        CancellationToken cancellationToken)
    {
        var existing = await _users.GetIamUserByEmailAsync(email, _clock.UtcNow, cancellationToken);
        if (existing is null)
            return;

        var result = await _createProfile.ExecuteAsync(existing.Id, fullName, cancellationToken);
        if (result.IsFailure)
        {
            _logger.LogWarning(
                "LocalDevFixtures CustomerProfile failed for {Email}: {Code}",
                email,
                result.Error?.Code);
        }
    }

    private async Task EnsureUserWithRoleAsync(
        string userName,
        string email,
        string password,
        Guid? roleId,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var existing = await _users.GetIamUserByEmailAsync(email, now, cancellationToken);
        Guid userId;

        if (existing is null)
        {
            var register = await _register.ExecuteAsync(
                new RegisterRequest
                {
                    UserName = userName,
                    Email = email,
                    Password = password
                },
                cancellationToken);

            if (!register.IsSuccess || register.Value is null)
            {
                _logger.LogWarning(
                    "LocalDevFixtures register failed for {Email}: {Code} {Detail}",
                    email,
                    register.Error?.Code,
                    register.Error?.Description);
                return;
            }

            userId = register.Value.UserId;
            _logger.LogInformation("LocalDevFixtures registered {Email}.", email);
        }
        else
        {
            userId = existing.Id;
            _logger.LogInformation("LocalDevFixtures user {Email} already exists.", email);
        }

        // Fixture accounts must be login-ready under RequireConfirmedEmail.
        await _emailConfirmation.MarkEmailConfirmedAsync(userId, cancellationToken);

        if (roleId is null)
            return;

        var added = await _userRoleWriter.AddToRoleAsync(userId, roleId.Value, cancellationToken);
        if (added)
            _logger.LogInformation("LocalDevFixtures assigned role to {Email}.", email);
    }
}
