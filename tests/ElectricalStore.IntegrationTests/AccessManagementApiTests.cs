using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ElectricalStore.IntegrationTests.Support;
using Xunit;

namespace ElectricalStore.IntegrationTests;

public sealed class AccessManagementApiTests : IClassFixture<AppWebApplicationFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AppWebApplicationFactory _factory;

    public AccessManagementApiTests(AppWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Unauthenticated_AccessEndpoints_Return401()
    {
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/admin/access/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/admin/access/roles")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/admin/access/permissions")).StatusCode);
    }

    [Fact]
    public async Task AccessManagement_HasNoUserCreateEndpoint_UnderRequireConfirmedEmail()
    {
        // Invariant: IAM UI/API currently manages existing users only.
        // Future staff provisioning must include verification email or a trusted admin confirm path
        // before login can succeed under RequireConfirmedEmail=true.
        var client = _factory.CreateClient();
        var swagger = await client.GetAsync("/swagger/v1/swagger.json");
        swagger.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await swagger.Content.ReadAsStringAsync());
        var usersPath = doc.RootElement.GetProperty("paths").GetProperty("/admin/access/users");
        Assert.True(usersPath.TryGetProperty("get", out _));
        Assert.False(
            usersPath.TryGetProperty("post", out _),
            "Do not add POST /admin/access/users without email-confirmation provisioning policy.");

        var owner = await CreateOwnerClientAsync();
        var createAttempt = await owner.PostAsJsonAsync("/admin/access/users", new
        {
            email = "staff-provision@example.test",
            userName = "staffprovision",
            password = TestKeys.UserPassword
        });
        Assert.True(
            createAttempt.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed,
            $"Unexpected status for staff create attempt: {createAttempt.StatusCode}");
    }

    [Fact]
    public async Task AuthenticatedWithoutIamPermission_Returns403()
    {
        var user = await CreateRegisteredUserClientAsync($"staff-{Guid.NewGuid():N}"[..20]);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/admin/access/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/admin/access/roles")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/admin/access/permissions")).StatusCode);
    }

    [Fact]
    public async Task Owner_ReadsUsersRolesPermissions_AndUserDetails_AreDashboardSafe()
    {
        var owner = await CreateOwnerClientAsync();

        // Register a user so the users list is non-empty (Owner is excluded from manageable list).
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var registered = await CreateRegisteredUserClientAsync($"r{suffix}", $"r{suffix}@example.test");
        var targetUserId = await GetUserIdFromMeAsync(registered);

        var users = await owner.GetAsync($"/admin/access/users?page=1&pageSize=20&search=r{suffix}");
        Assert.Equal(HttpStatusCode.OK, users.StatusCode);
        using (var doc = JsonDocument.Parse(await users.Content.ReadAsStringAsync()))
        {
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("items", out var items));
            Assert.True(items.GetArrayLength() >= 1);
            var first = items[0];
            Assert.True(first.TryGetProperty("id", out _));
            Assert.True(first.TryGetProperty("userName", out _));
            Assert.True(first.TryGetProperty("email", out _));
            Assert.False(first.TryGetProperty("passwordHash", out _));
            Assert.False(first.TryGetProperty("securityStamp", out _));
            Assert.False(first.TryGetProperty("concurrencyStamp", out _));
        }

        var roles = await owner.GetAsync("/admin/access/roles");
        Assert.Equal(HttpStatusCode.OK, roles.StatusCode);
        using (var doc = JsonDocument.Parse(await roles.Content.ReadAsStringAsync()))
        {
            var arr = doc.RootElement;
            Assert.True(arr.GetArrayLength() >= 1);
            var ownerRole = arr.EnumerateArray().First(r =>
                string.Equals(r.GetProperty("name").GetString(), "Owner", StringComparison.OrdinalIgnoreCase));
            // Permixa bootstrap: Owner RoleLevel is 1 (lower number = higher authority).
            Assert.Equal(1, ownerRole.GetProperty("roleLevel").GetInt32());
        }

        var myRoles = await owner.GetAsync("/admin/access/me/roles");
        Assert.Equal(HttpStatusCode.OK, myRoles.StatusCode);

        var permissions = await owner.GetAsync("/admin/access/permissions?search=Iam.Users");
        Assert.Equal(HttpStatusCode.OK, permissions.StatusCode);
        using (var doc = JsonDocument.Parse(await permissions.Content.ReadAsStringAsync()))
        {
            Assert.True(doc.RootElement.TryGetProperty("groups", out var groups));
            Assert.True(groups.GetArrayLength() >= 1);
        }

        // Self IAM detail is forbidden by Permixa hierarchy (CannotManageSelf).
        var ownerId = (await _factory.CreateClient().LoginAsOwnerAsync()).UserId;
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync($"/admin/access/users/{ownerId}")).StatusCode);

        var detail = await owner.GetAsync($"/admin/access/users/{targetUserId}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        using (var doc = JsonDocument.Parse(await detail.Content.ReadAsStringAsync()))
        {
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("user", out _));
            Assert.True(root.TryGetProperty("roles", out _));
            Assert.True(root.TryGetProperty("overrides", out _));
            Assert.True(root.TryGetProperty("permissions", out var perms));
            Assert.Contains(perms.EnumerateArray(), p =>
                p.GetProperty("code").GetString() == "Categories.Manage" &&
                !p.GetProperty("effective").GetBoolean() &&
                p.GetProperty("source").GetString() == "DefaultDeny");
            Assert.False(root.GetProperty("user").TryGetProperty("passwordHash", out _));
        }
    }

    [Fact]
    public async Task RoleLifecycle_CreateRenameDuplicateConflict_AndPermissionGrantRevoke()
    {
        var owner = await CreateOwnerClientAsync();
        var ownerRoleId = await FindRoleIdAsync(owner, "Owner");
        var roleName = $"Mgr-{Guid.NewGuid():N}"[..12];

        var create = await owner.PostAsJsonAsync("/admin/access/roles", new
        {
            name = roleName,
            referenceRoleId = ownerRoleId,
            placement = "Below"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var createdDoc = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var roleId = createdDoc.RootElement.GetProperty("id").GetGuid();
        Assert.True(createdDoc.RootElement.GetProperty("roleLevel").GetInt32() > 1);

        var duplicate = await owner.PostAsJsonAsync("/admin/access/roles", new
        {
            name = roleName,
            referenceRoleId = ownerRoleId,
            placement = "Below"
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var renamed = $"{roleName}-x";
        var update = await owner.PutAsJsonAsync($"/admin/access/roles/{roleId}", new { name = renamed });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var sampleReadId = await FindPermissionIdAsync(owner, "Categories.Manage");
        var grant = await owner.PutAsJsonAsync($"/admin/access/roles/{roleId}/permissions", new
        {
            permissionIds = new[] { sampleReadId }
        });
        Assert.Equal(HttpStatusCode.OK, grant.StatusCode);
        using (var doc = JsonDocument.Parse(await grant.Content.ReadAsStringAsync()))
        {
            Assert.Contains(doc.RootElement.EnumerateArray(), p => p.GetProperty("id").GetGuid() == sampleReadId);
        }

        var revoke = await owner.PutAsJsonAsync($"/admin/access/roles/{roleId}/permissions", new
        {
            permissionIds = Array.Empty<Guid>()
        });
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);
        using (var doc = JsonDocument.Parse(await revoke.Content.ReadAsStringAsync()))
        {
            Assert.Equal(0, doc.RootElement.GetArrayLength());
        }

        var delete = await owner.DeleteAsync($"/admin/access/roles/{roleId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task UserRoles_AssignRemove_AndDuplicateAssignmentHandled()
    {
        var owner = await CreateOwnerClientAsync();
        var ownerRoleId = await FindRoleIdAsync(owner, "Owner");
        var roleName = $"Role-{Guid.NewGuid():N}"[..12];
        var roleId = await CreateRoleBelowOwnerAsync(owner, ownerRoleId, roleName);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var userClient = await CreateRegisteredUserClientAsync($"u{suffix}", $"u{suffix}@example.test");
        var userId = await GetUserIdFromMeAsync(userClient);

        var assign = await owner.PutAsJsonAsync($"/admin/access/users/{userId}/roles", new
        {
            roleIds = new[] { roleId }
        });
        Assert.Equal(HttpStatusCode.OK, assign.StatusCode);

        var again = await owner.PutAsJsonAsync($"/admin/access/users/{userId}/roles", new
        {
            roleIds = new[] { roleId }
        });
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);

        var clear = await owner.PutAsJsonAsync($"/admin/access/users/{userId}/roles", new
        {
            roleIds = Array.Empty<Guid>()
        });
        Assert.Equal(HttpStatusCode.OK, clear.StatusCode);
        using var doc = JsonDocument.Parse(await clear.Content.ReadAsStringAsync());
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task UserOverrides_AllowDenyRemove_AndPrecedence_UserDenyOverRole()
    {
        var owner = await CreateOwnerClientAsync();
        var ownerRoleId = await FindRoleIdAsync(owner, "Owner");
        var roleId = await CreateRoleBelowOwnerAsync(owner, ownerRoleId, $"Pr-{Guid.NewGuid():N}"[..10]);
        var categoriesManageId = await FindPermissionIdAsync(owner, "Categories.Manage");

        await owner.PutAsJsonAsync($"/admin/access/roles/{roleId}/permissions", new
        {
            permissionIds = new[] { categoriesManageId }
        });

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var userClient = await CreateRegisteredUserClientAsync($"p{suffix}", $"p{suffix}@example.test");
        var userId = await GetUserIdFromMeAsync(userClient);

        await owner.PutAsJsonAsync($"/admin/access/users/{userId}/roles", new
        {
            roleIds = new[] { roleId }
        });

        var effectiveAfterRole = await owner.GetAsync($"/admin/access/users/{userId}/permissions");
        Assert.Equal(HttpStatusCode.OK, effectiveAfterRole.StatusCode);
        AssertPermissionSource(await effectiveAfterRole.Content.ReadAsStringAsync(), "Categories.Manage", effective: true, "Role");

        var deny = await owner.PutAsJsonAsync(
            $"/admin/access/users/{userId}/permissions/{categoriesManageId}",
            new { effect = "Deny" });
        Assert.Equal(HttpStatusCode.OK, deny.StatusCode);

        var afterDeny = await owner.GetAsync($"/admin/access/users/{userId}/permissions");
        AssertPermissionSource(await afterDeny.Content.ReadAsStringAsync(), "Categories.Manage", effective: false, "UserDeny");

        var removeDeny = await owner.DeleteAsync($"/admin/access/users/{userId}/permissions/{categoriesManageId}");
        Assert.Equal(HttpStatusCode.NoContent, removeDeny.StatusCode);

        var afterInherit = await owner.GetAsync($"/admin/access/users/{userId}/permissions");
        AssertPermissionSource(await afterInherit.Content.ReadAsStringAsync(), "Categories.Manage", effective: true, "Role");

        var productsManageId = await FindPermissionIdAsync(owner, "Products.Manage");
        var allow = await owner.PutAsJsonAsync(
            $"/admin/access/users/{userId}/permissions/{productsManageId}",
            new { effect = "Allow" });
        Assert.Equal(HttpStatusCode.OK, allow.StatusCode);

        var afterAllow = await owner.GetAsync($"/admin/access/users/{userId}/permissions");
        var afterAllowBody = await afterAllow.Content.ReadAsStringAsync();
        AssertPermissionSource(afterAllowBody, "Products.Manage", effective: true, "UserAllow");
        AssertPermissionSource(afterAllowBody, "Orders.Manage", effective: false, "DefaultDeny");
    }

    [Fact]
    public async Task Hierarchy_LowerAuthorityCannotManageHigher_OrSelfEscalate()
    {
        var owner = await CreateOwnerClientAsync();
        var ownerRoleId = await FindRoleIdAsync(owner, "Owner");
        var ownerUserId = (await _factory.CreateClient().LoginAsOwnerAsync()).UserId;

        var staffRoleId = await CreateRoleBelowOwnerAsync(owner, ownerRoleId, $"St-{Guid.NewGuid():N}"[..10]);

        var needed = new[]
        {
            "Iam.Users.Read",
            "Iam.Roles.Read",
            "Iam.Roles.Create",
            "Iam.Roles.Update",
            "Iam.UserRoles.Manage",
            "Iam.UserPermissionOverrides.Manage",
            "Iam.Permissions.Read"
        };
        var permissionIds = new List<Guid>();
        foreach (var name in needed)
            permissionIds.Add(await FindPermissionIdAsync(owner, name));

        var setPerms = await owner.PutAsJsonAsync($"/admin/access/roles/{staffRoleId}/permissions", new
        {
            permissionIds
        });
        Assert.Equal(HttpStatusCode.OK, setPerms.StatusCode);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var staffHttp = await CreateRegisteredUserClientAsync($"s{suffix}", $"s{suffix}@example.test");
        var staffUserId = await GetUserIdFromMeAsync(staffHttp);

        var assignStaff = await owner.PutAsJsonAsync($"/admin/access/users/{staffUserId}/roles", new
        {
            roleIds = new[] { staffRoleId }
        });
        Assert.Equal(HttpStatusCode.OK, assignStaff.StatusCode);

        var login = await _factory.CreateClient().PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = $"s{suffix}@example.test",
            password = TestKeys.UserPassword
        });
        login.EnsureSuccessStatusCode();
        var tokens = (await login.Content.ReadFromJsonAsync<AuthTokenResponse>(Json))!;
        var staff = _factory.CreateAuthenticatedClient(tokens.AccessToken);

        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/admin/access/users/{staffUserId}")).StatusCode);

        var escalateRoles = await staff.PutAsJsonAsync($"/admin/access/users/{ownerUserId}/roles", new
        {
            roleIds = new[] { staffRoleId }
        });
        Assert.Equal(HttpStatusCode.Forbidden, escalateRoles.StatusCode);

        var selfEscalate = await staff.PutAsJsonAsync($"/admin/access/users/{staffUserId}/roles", new
        {
            roleIds = new[] { ownerRoleId }
        });
        Assert.Equal(HttpStatusCode.Forbidden, selfEscalate.StatusCode);

        var createAbove = await staff.PostAsJsonAsync("/admin/access/roles", new
        {
            name = $"X-{Guid.NewGuid():N}"[..10],
            referenceRoleId = ownerRoleId,
            placement = "Above"
        });
        Assert.Equal(HttpStatusCode.Forbidden, createAbove.StatusCode);
    }

    [Fact]
    public async Task MissingUserOrRole_Returns404()
    {
        var owner = await CreateOwnerClientAsync();
        var missing = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/admin/access/users/{missing}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/admin/access/roles/{missing}")).StatusCode);
    }

    private async Task<HttpClient> CreateOwnerClientAsync()
    {
        var tokens = await _factory.CreateClient().LoginAsOwnerAsync();
        return _factory.CreateAuthenticatedClient(tokens.AccessToken);
    }

    private async Task<HttpClient> CreateRegisteredUserClientAsync(string userName, string? email = null)
    {
        email ??= $"{userName}@example.test";
        var client = _factory.CreateClient();
        var register = await client.PostAsJsonAsync("/auth/register", new
        {
            userName,
            email,
            password = TestKeys.UserPassword
        });
        register.EnsureSuccessStatusCode();

        await _factory.MarkEmailConfirmedAsync(email);

        var login = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = email,
            password = TestKeys.UserPassword
        });
        login.EnsureSuccessStatusCode();
        var tokens = (await login.Content.ReadFromJsonAsync<AuthTokenResponse>(Json))!;
        return _factory.CreateAuthenticatedClient(tokens.AccessToken);
    }

    private static async Task<Guid> GetUserIdFromMeAsync(HttpClient client)
    {
        var me = await client.GetAsync("/me");
        me.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("userId").GetGuid();
    }

    private static async Task<Guid> FindRoleIdAsync(HttpClient owner, string name)
    {
        var roles = await owner.GetAsync("/admin/access/roles");
        roles.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await roles.Content.ReadAsStringAsync());
        return doc.RootElement.EnumerateArray()
            .First(r => string.Equals(r.GetProperty("name").GetString(), name, StringComparison.OrdinalIgnoreCase))
            .GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateRoleBelowOwnerAsync(HttpClient owner, Guid ownerRoleId, string name)
    {
        var create = await owner.PostAsJsonAsync("/admin/access/roles", new
        {
            name,
            referenceRoleId = ownerRoleId,
            placement = "Below"
        });
        create.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> FindPermissionIdAsync(HttpClient owner, string code)
    {
        var response = await owner.GetAsync($"/admin/access/permissions?search={Uri.EscapeDataString(code)}");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        foreach (var group in doc.RootElement.GetProperty("groups").EnumerateArray())
        {
            foreach (var p in group.GetProperty("permissions").EnumerateArray())
            {
                if (string.Equals(p.GetProperty("code").GetString(), code, StringComparison.Ordinal))
                    return p.GetProperty("id").GetGuid();
            }
        }

        throw new InvalidOperationException($"Permission '{code}' not found.");
    }

    private static void AssertPermissionSource(string json, string code, bool effective, string source)
    {
        using var doc = JsonDocument.Parse(json);
        var item = doc.RootElement.GetProperty("permissions").EnumerateArray()
            .First(p => p.GetProperty("code").GetString() == code);
        Assert.Equal(effective, item.GetProperty("effective").GetBoolean());
        Assert.Equal(source, item.GetProperty("source").GetString());
    }
}
