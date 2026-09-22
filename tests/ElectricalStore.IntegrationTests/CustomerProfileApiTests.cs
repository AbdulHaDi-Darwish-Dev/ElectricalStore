using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ElectricalStore.Infrastructure.Persistence;
using ElectricalStore.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElectricalStore.IntegrationTests;

public sealed class CustomerProfileApiTests : IClassFixture<AppWebApplicationFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AppWebApplicationFactory _factory;

    public CustomerProfileApiTests(AppWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Anonymous_GetProfile_Returns401()
    {
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/account/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync("/account/profile", new { fullName = "X" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/account/change-password", new
        {
            currentPassword = "a",
            newPassword = "b"
        })).StatusCode);
    }

    [Fact]
    public async Task RegisterCustomer_ArabicFullName_ThenProfileAndPasswordChange()
    {
        var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"cust-{suffix}@example.test";
        const string password = "Customer-Test-Password-1!";
        const string fullName = "عبدالهادي درويش";

        var register = await client.PostAsJsonAsync("/account/register", new
        {
            fullName,
            email,
            password
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var registered = (await register.Content.ReadFromJsonAsync<RegisterBody>(Json))!;
        Assert.Equal(fullName, registered.FullName);
        Assert.Equal(email, registered.Email);
        Assert.True(registered.EmailVerificationRequired);
        Assert.True(registered.VerificationEmailSent);

        await _factory.ConfirmEmailFromOutboxAsync(client, email);

        var login = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = email,
            password
        });
        login.EnsureSuccessStatusCode();
        var tokens = (await login.Content.ReadFromJsonAsync<AuthTokenResponse>(Json))!;
        var authed = _factory.CreateAuthenticatedClient(tokens.AccessToken);

        var me = await authed.GetAsync("/me");
        me.EnsureSuccessStatusCode();
        using (var doc = JsonDocument.Parse(await me.Content.ReadAsStringAsync()))
        {
            Assert.True(doc.RootElement.TryGetProperty("userId", out _));
            Assert.True(doc.RootElement.TryGetProperty("permissions", out _));
            Assert.False(doc.RootElement.TryGetProperty("fullName", out _));
            Assert.False(doc.RootElement.TryGetProperty("email", out _));
        }

        var profile = await authed.GetAsync("/account/profile");
        profile.EnsureSuccessStatusCode();
        var profileDto = (await profile.Content.ReadFromJsonAsync<ProfileBody>(Json))!;
        Assert.Equal(fullName, profileDto.FullName);
        Assert.Equal(email, profileDto.Email);

        const string updatedName = "عبدالهادي م. درويش";
        var update = await authed.PutAsJsonAsync("/account/profile", new { fullName = updatedName });
        update.EnsureSuccessStatusCode();
        var updated = (await update.Content.ReadFromJsonAsync<ProfileBody>(Json))!;
        Assert.Equal(updatedName, updated.FullName);
        Assert.Equal(email, updated.Email);

        // Profile update must not accept email mutation via unknown fields (ignored by binder).
        var sneaky = await authed.PutAsJsonAsync("/account/profile", new
        {
            fullName = updatedName,
            email = "attacker@example.test",
            userName = "hacker"
        });
        sneaky.EnsureSuccessStatusCode();
        var afterSneaky = (await (await authed.GetAsync("/account/profile")).Content
            .ReadFromJsonAsync<ProfileBody>(Json))!;
        Assert.Equal(email, afterSneaky.Email);

        const string newPassword = "Customer-Test-Password-2!";
        var change = await authed.PostAsJsonAsync("/account/change-password", new
        {
            currentPassword = password,
            newPassword
        });
        change.EnsureSuccessStatusCode();

        var badLogin = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = email,
            password
        });
        Assert.Equal(HttpStatusCode.Unauthorized, badLogin.StatusCode);

        var goodLogin = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = email,
            password = newPassword
        });
        goodLogin.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task LegacyIdentityWithoutProfile_GetIsReadOnly_PutCreates()
    {
        var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"legacy-{suffix}@example.test";

        // Identity-only register (no CustomerProfile).
        (await client.PostAsJsonAsync("/auth/register", new
        {
            userName = $"legacy{suffix}",
            email,
            password = TestKeys.UserPassword
        })).EnsureSuccessStatusCode();

        await _factory.MarkEmailConfirmedAsync(email);

        var login = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = email,
            password = TestKeys.UserPassword
        });
        login.EnsureSuccessStatusCode();
        var tokens = (await login.Content.ReadFromJsonAsync<AuthTokenResponse>(Json))!;
        var authed = _factory.CreateAuthenticatedClient(tokens.AccessToken);

        var profile = await authed.GetAsync("/account/profile");
        profile.EnsureSuccessStatusCode();
        var dto = (await profile.Content.ReadFromJsonAsync<ProfileBody>(Json))!;
        Assert.Equal(email, dto.Email);
        // Provisional name is email local-part — not a fabricated Arabic name.
        Assert.Equal($"legacy-{suffix}", dto.FullName);
        var userId = dto.UserId;
        Assert.NotEqual(Guid.Empty, userId);

        // GET must not persist a CustomerProfile row.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.CustomerProfiles.AnyAsync(p => p.UserId == userId));
        }

        const string realName = "عبدالهادي درويش";
        var update = await authed.PutAsJsonAsync("/account/profile", new { fullName = realName });
        update.EnsureSuccessStatusCode();
        var updated = (await update.Content.ReadFromJsonAsync<ProfileBody>(Json))!;
        Assert.Equal(realName, updated.FullName);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var row = await db.CustomerProfiles.SingleAsync(p => p.UserId == userId);
            Assert.Equal(realName, row.FullName);
        }

        var again = await (await authed.GetAsync("/account/profile")).Content
            .ReadFromJsonAsync<ProfileBody>(Json);
        Assert.Equal(realName, again!.FullName);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_ReturnsValidationProblem()
    {
        var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"pw-{suffix}@example.test";
        (await client.PostAsJsonAsync("/account/register", new
        {
            fullName = "Test User",
            email,
            password = TestKeys.UserPassword
        })).EnsureSuccessStatusCode();

        await _factory.ConfirmEmailFromOutboxAsync(client, email);

        var login = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = email,
            password = TestKeys.UserPassword
        });
        var tokens = (await login.Content.ReadFromJsonAsync<AuthTokenResponse>(Json))!;
        var authed = _factory.CreateAuthenticatedClient(tokens.AccessToken);

        var bad = await authed.PostAsJsonAsync("/account/change-password", new
        {
            currentPassword = "Wrong-Password-1!",
            newPassword = "Customer-Test-Password-9!"
        });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    private sealed class RegisterBody
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool EmailVerificationRequired { get; set; }
        public bool VerificationEmailSent { get; set; }
    }

    private sealed class ProfileBody
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool EmailConfirmed { get; set; }
    }
}
