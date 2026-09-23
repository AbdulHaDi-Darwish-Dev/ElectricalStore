using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using ElectricalStore.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Permixa.Application.Authentication.Abstractions;
using Permixa.Application.Identity.Abstractions;
using Permixa.Infrastructure.Identity;
using Permixa.Infrastructure.Persistence.Repositories;
using Xunit;

namespace ElectricalStore.IntegrationTests;

public sealed class PasswordResetApiTests : IClassFixture<AppWebApplicationFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Regex ResetLinkRegex = new(
        @"https?://[^\s]+/reset-password\?challengeId=([0-9a-fA-F-]{36})&token=([^\s]+)",
        RegexOptions.Compiled);

    private readonly AppWebApplicationFactory _factory;

    public PasswordResetApiTests(AppWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Forgot_Reset_OldPasswordFails_NewPasswordSucceeds_AndTokenReuseBlocked()
    {
        var client = _factory.CreateClient();
        _factory.Emails.Clear();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"reset-{suffix}@example.test";
        const string oldPassword = "Customer-Reset-Old-1!";
        const string newPassword = "Customer-Reset-New-2!";
        const string fullName = "عبدالهادي درويش";

        var register = await client.PostAsJsonAsync("/account/register", new
        {
            fullName,
            email,
            password = oldPassword
        });
        register.EnsureSuccessStatusCode();
        var registered = (await register.Content.ReadFromJsonAsync<RegisterBody>(Json))!;

        await _factory.ConfirmEmailFromOutboxAsync(client, email);
        _factory.Emails.Clear();

        string? technicalUserName;
        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(registered.UserId.ToString());
            Assert.NotNull(user);
            technicalUserName = user!.UserName;
            Assert.False(string.IsNullOrWhiteSpace(technicalUserName));
            Assert.StartsWith("customer-", technicalUserName, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain('@', technicalUserName!);
        }

        var loginA = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = email,
            password = oldPassword
        });
        loginA.EnsureSuccessStatusCode();
        var sessionA = (await loginA.Content.ReadFromJsonAsync<AuthTokenResponse>(Json))!;

        // Second login → second refresh family (prove RevokeAll, not single-token revoke).
        var loginB = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = email,
            password = oldPassword
        });
        loginB.EnsureSuccessStatusCode();
        var sessionB = (await loginB.Content.ReadFromJsonAsync<AuthTokenResponse>(Json))!;
        Assert.NotEqual(sessionA.RefreshToken, sessionB.RefreshToken);

        var accessLifetime = ReadAccessTokenLifetime(sessionA.AccessToken);
        Assert.True(
            accessLifetime >= TimeSpan.FromMinutes(10) && accessLifetime <= TimeSpan.FromHours(2),
            $"Unexpected access JWT lifetime: {accessLifetime}");

        var forgot = await PostForgotAsync(client, email);
        forgot.EnsureSuccessStatusCode();
        var forgotBody = (await forgot.Content.ReadFromJsonAsync<ForgotBody>(Json))!;
        Assert.Equal(
            ElectricalStore.Application.Customers.RequestCustomerPasswordResetUseCase.PublicGenericMessage,
            forgotBody.Message);

        var (challengeId, token) = ExtractResetChallenge(AwaitLatestResetEmail(email));

        var reset = await client.PostAsJsonAsync("/account/password/reset", new
        {
            challengeId,
            token,
            newPassword
        });
        reset.EnsureSuccessStatusCode();
        using (var doc = JsonDocument.Parse(await reset.Content.ReadAsStringAsync()))
        {
            Assert.True(doc.RootElement.GetProperty("reset").GetBoolean());
            Assert.False(doc.RootElement.TryGetProperty("accessToken", out _));
            Assert.False(doc.RootElement.TryGetProperty("refreshToken", out _));
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(registered.UserId.ToString());
            Assert.NotNull(user);
            Assert.Equal(technicalUserName, user!.UserName);
            Assert.DoesNotContain('@', user.UserName!);
        }

        var oldLogin = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = email,
            password = oldPassword
        });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var newLogin = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = email,
            password = newPassword
        });
        newLogin.EnsureSuccessStatusCode();

        var reuse = await client.PostAsJsonAsync("/account/password/reset", new
        {
            challengeId,
            token,
            newPassword = "Customer-Reset-New-3!"
        });
        Assert.Equal(HttpStatusCode.Conflict, reuse.StatusCode);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = sessionA.RefreshToken }))
                .StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = sessionB.RefreshToken }))
                .StatusCode);

        // Stateless access JWT remains valid until expiry.
        var authed = _factory.CreateAuthenticatedClient(sessionA.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await authed.GetAsync("/me")).StatusCode);

        var profile = await (await _factory.CreateAuthenticatedClient(
                (await newLogin.Content.ReadFromJsonAsync<AuthTokenResponse>(Json))!.AccessToken)
            .GetAsync("/account/profile")).Content.ReadFromJsonAsync<ProfileBody>(Json);
        Assert.Equal(fullName, profile!.FullName);
        Assert.Equal(email, profile.Email);
        Assert.True(profile.EmailConfirmed);
    }

    [Fact]
    public async Task Forgot_UnknownEmail_IsAntiEnumeration_NoEmail()
    {
        var client = _factory.CreateClient();
        _factory.Emails.Clear();

        var response = await PostForgotAsync(
            client,
            $"missing-{Guid.NewGuid():N}@example.test");
        response.EnsureSuccessStatusCode();
        var body = (await response.Content.ReadFromJsonAsync<ForgotBody>(Json))!;
        Assert.Equal(
            ElectricalStore.Application.Customers.RequestCustomerPasswordResetUseCase.PublicGenericMessage,
            body.Message);
        Assert.Empty(_factory.Emails.Sent);
    }

    [Fact]
    public async Task Reset_InvalidToken_ReturnsSafeBadRequest()
    {
        var client = _factory.CreateClient();
        _factory.Emails.Clear();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"badtok-{suffix}@example.test";
        const string password = "Customer-Reset-Pass-1!";

        (await client.PostAsJsonAsync("/account/register", new
        {
            fullName = "Bad Token",
            email,
            password
        })).EnsureSuccessStatusCode();
        await _factory.ConfirmEmailFromOutboxAsync(client, email);
        _factory.Emails.Clear();

        (await PostForgotAsync(client, email)).EnsureSuccessStatusCode();
        var (challengeId, _) = ExtractResetChallenge(AwaitLatestResetEmail(email));

        var bad = await client.PostAsJsonAsync("/account/password/reset", new
        {
            challengeId,
            token = "not-a-valid-token",
            newPassword = "Customer-Reset-Pass-2!"
        });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task Forgot_UnconfirmedAccount_ResetDoesNotConfirmEmail_LoginStillBlocked()
    {
        var client = _factory.CreateClient();
        _factory.Emails.Clear();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"unconf-reset-{suffix}@example.test";
        const string oldPassword = "Customer-Unconf-Old-1!";
        const string newPassword = "Customer-Unconf-New-2!";

        var register = await client.PostAsJsonAsync("/account/register", new
        {
            fullName = "Unconfirmed Reset",
            email,
            password = oldPassword
        });
        register.EnsureSuccessStatusCode();
        var registered = (await register.Content.ReadFromJsonAsync<RegisterBody>(Json))!;
        _factory.Emails.Clear();

        var forgot = await PostForgotAsync(client, email);
        forgot.EnsureSuccessStatusCode();
        Assert.NotEmpty(_factory.Emails.Sent);

        var (challengeId, token) = ExtractResetChallenge(AwaitLatestResetEmail(email));
        (await client.PostAsJsonAsync("/account/password/reset", new
        {
            challengeId,
            token,
            newPassword
        })).EnsureSuccessStatusCode();

        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(registered.UserId.ToString());
            Assert.NotNull(user);
            Assert.False(user!.EmailConfirmed);
        }

        var login = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = email,
            password = newPassword
        });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        using var doc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        Assert.Equal(
            "Authentication.EmailNotConfirmed",
            doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Forgot_DisabledAccount_NoEmail_GenericSuccess()
    {
        var client = _factory.CreateClient();
        _factory.Emails.Clear();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"disabled-reset-{suffix}@example.test";
        const string password = "Customer-Disabled-1!";

        var register = await client.PostAsJsonAsync("/account/register", new
        {
            fullName = "Disabled User",
            email,
            password
        });
        register.EnsureSuccessStatusCode();
        var registered = (await register.Content.ReadFromJsonAsync<RegisterBody>(Json))!;
        await _factory.ConfirmEmailFromOutboxAsync(client, email);
        _factory.Emails.Clear();

        using (var scope = _factory.Services.CreateScope())
        {
            var writer = scope.ServiceProvider.GetRequiredService<IIdentityUserWriter>();
            var uow = scope.ServiceProvider.GetRequiredService<Permixa.Application.Common.Abstractions.IUnitOfWork>();
            await writer.SetDisabledAsync(registered.UserId, true);
            await uow.SaveChangesAsync();
        }

        var forgot = await PostForgotAsync(client, email);
        forgot.EnsureSuccessStatusCode();
        Assert.Empty(_factory.Emails.Sent);
    }

    [Fact]
    public async Task Reset_ChallengeIssuedThenAccountDisabled_RejectsReset_KeepsDisabled()
    {
        var client = _factory.CreateClient();
        _factory.Emails.Clear();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"disable-after-{suffix}@example.test";
        const string oldPassword = "Customer-Disable-After-1!";
        const string newPassword = "Customer-Disable-After-2!";

        var register = await client.PostAsJsonAsync("/account/register", new
        {
            fullName = "Disable After Challenge",
            email,
            password = oldPassword
        });
        register.EnsureSuccessStatusCode();
        var registered = (await register.Content.ReadFromJsonAsync<RegisterBody>(Json))!;
        await _factory.ConfirmEmailFromOutboxAsync(client, email);
        _factory.Emails.Clear();

        (await PostForgotAsync(client, email)).EnsureSuccessStatusCode();
        var (challengeId, token) = ExtractResetChallenge(AwaitLatestResetEmail(email));

        using (var scope = _factory.Services.CreateScope())
        {
            var writer = scope.ServiceProvider.GetRequiredService<IIdentityUserWriter>();
            var uow = scope.ServiceProvider.GetRequiredService<Permixa.Application.Common.Abstractions.IUnitOfWork>();
            await writer.SetDisabledAsync(registered.UserId, true);
            await uow.SaveChangesAsync();
        }

        var reset = await client.PostAsJsonAsync("/account/password/reset", new
        {
            challengeId,
            token,
            newPassword
        });
        Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var readers = scope.ServiceProvider.GetRequiredService<IIdentityUserReader>();
            var clock = scope.ServiceProvider.GetRequiredService<Permixa.Application.Common.Abstractions.IClock>();
            var state = await readers.GetAccountStateAsync(registered.UserId, clock.UtcNow);
            Assert.True(state!.IsDisabled);
        }

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/auth/login", new
            {
                emailOrUserName = email,
                password = oldPassword
            })).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/auth/login", new
            {
                emailOrUserName = email,
                password = newPassword
            })).StatusCode);
    }

    [Fact]
    public async Task Reset_LockedAccount_PreservesLockout_DoesNotUnlock()
    {
        var client = _factory.CreateClient();
        _factory.Emails.Clear();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"lockout-reset-{suffix}@example.test";
        const string oldPassword = "Customer-Lockout-Old-1!";
        const string newPassword = "Customer-Lockout-New-2!";

        var register = await client.PostAsJsonAsync("/account/register", new
        {
            fullName = "Lockout User",
            email,
            password = oldPassword
        });
        register.EnsureSuccessStatusCode();
        var registered = (await register.Content.ReadFromJsonAsync<RegisterBody>(Json))!;
        await _factory.ConfirmEmailFromOutboxAsync(client, email);
        _factory.Emails.Clear();

        int accessFailedBefore;
        DateTimeOffset? lockoutEndBefore;
        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(registered.UserId.ToString());
            Assert.NotNull(user);
            await users.SetLockoutEnabledAsync(user!, true);
            await users.SetLockoutEndDateAsync(user!, DateTimeOffset.UtcNow.AddHours(1));
            await users.AccessFailedAsync(user!);
            await users.AccessFailedAsync(user!);
            accessFailedBefore = await users.GetAccessFailedCountAsync(user!);
            lockoutEndBefore = await users.GetLockoutEndDateAsync(user!);
            Assert.True(accessFailedBefore >= 1);
            Assert.True(lockoutEndBefore > DateTimeOffset.UtcNow);
        }

        (await PostForgotAsync(client, email)).EnsureSuccessStatusCode();
        var (challengeId, token) = ExtractResetChallenge(AwaitLatestResetEmail(email));
        (await client.PostAsJsonAsync("/account/password/reset", new
        {
            challengeId,
            token,
            newPassword
        })).EnsureSuccessStatusCode();

        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(registered.UserId.ToString());
            Assert.NotNull(user);
            var accessFailedAfter = await users.GetAccessFailedCountAsync(user!);
            var lockoutEndAfter = await users.GetLockoutEndDateAsync(user!);

            // Observed: Permixa/Identity password-reset does NOT clear lockout for this host.
            Assert.Equal(accessFailedBefore, accessFailedAfter);
            Assert.NotNull(lockoutEndAfter);
            Assert.True(lockoutEndAfter > DateTimeOffset.UtcNow);
            Assert.True(
                lockoutEndBefore!.Value - lockoutEndAfter!.Value < TimeSpan.FromMinutes(1)
                || lockoutEndAfter > DateTimeOffset.UtcNow);
        }

        // Still locked → login with new password must fail until lockout ends.
        var lockedLogin = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = email,
            password = newPassword
        });
        Assert.Equal(HttpStatusCode.Unauthorized, lockedLogin.StatusCode);
    }

    [Fact]
    public async Task Forgot_RateLimit_Returns429_AfterRemoteIpPermitExhausted()
    {
        var client = _factory.CreateClient();
        const string isolatedIp = "198.51.100.88";

        HttpResponseMessage? last = null;
        for (var i = 0; i < 6; i++)
        {
            last?.Dispose();
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/account/password/forgot")
            {
                Content = JsonContent.Create(new
                {
                    email = $"pw-rl-{i}@example.test"
                })
            };
            request.Headers.TryAddWithoutValidation(
                ElectricalStore.Api.Hosting.TestConnectingIpStartupFilter.HeaderName,
                isolatedIp);
            last = await client.SendAsync(request);
        }

        Assert.NotNull(last);
        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
    }

    [Fact]
    public async Task Reset_WeakPassword_ReturnsPolicyFailure()
    {
        var client = _factory.CreateClient();
        _factory.Emails.Clear();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"weakpw-{suffix}@example.test";
        const string password = "Customer-Reset-Pass-9!";

        (await client.PostAsJsonAsync("/account/register", new
        {
            fullName = "Weak Password",
            email,
            password
        })).EnsureSuccessStatusCode();
        await _factory.ConfirmEmailFromOutboxAsync(client, email);
        _factory.Emails.Clear();

        (await PostForgotAsync(client, email)).EnsureSuccessStatusCode();
        var (challengeId, token) = ExtractResetChallenge(AwaitLatestResetEmail(email));

        var weak = await client.PostAsJsonAsync("/account/password/reset", new
        {
            challengeId,
            token,
            newPassword = "short"
        });
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
    }

    [Fact]
    public async Task CrossPurpose_EmailConfirmationToken_CannotResetPassword()
    {
        var client = _factory.CreateClient();
        _factory.Emails.Clear();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"xconf-pw-{suffix}@example.test";
        const string password = "Customer-CrossConf-Pw-1!";

        (await client.PostAsJsonAsync("/account/register", new
        {
            fullName = "Cross Conf PW",
            email,
            password
        })).EnsureSuccessStatusCode();

        var verifyMail = _factory.Emails.Sent.Last(e =>
            string.Equals(e.To, email, StringComparison.OrdinalIgnoreCase)
            && e.TextBody.Contains("/verify-email", StringComparison.OrdinalIgnoreCase));
        var match = new Regex(
            @"challengeId=([0-9a-fA-F-]{36})&token=([^\s]+)",
            RegexOptions.Compiled).Match(verifyMail.TextBody);
        Assert.True(match.Success);
        var challengeId = Guid.Parse(match.Groups[1].Value);
        var token = Uri.UnescapeDataString(match.Groups[2].Value);

        var reset = await client.PostAsJsonAsync("/account/password/reset", new
        {
            challengeId,
            token,
            newPassword = "Customer-CrossConf-Pw-2!"
        });
        Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);

        // Registration verification still works afterward.
        (await client.PostAsJsonAsync("/account/email-verification/confirm", new
        {
            challengeId,
            token
        })).EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = email,
            password
        });
        login.EnsureSuccessStatusCode();
    }

    private async Task<HttpResponseMessage> PostForgotAsync(
        HttpClient client,
        string email,
        string? connectingIp = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/account/password/forgot")
        {
            Content = JsonContent.Create(new { email })
        };
        request.Headers.TryAddWithoutValidation(
            ElectricalStore.Api.Hosting.TestConnectingIpStartupFilter.HeaderName,
            connectingIp ?? $"203.0.113.{Random.Shared.Next(1, 254)}");
        return await client.SendAsync(request);
    }

    private CapturedEmail AwaitLatestResetEmail(string to)
    {
        var match = _factory.Emails.Sent.LastOrDefault(e =>
            string.Equals(e.To, to, StringComparison.OrdinalIgnoreCase)
            && e.TextBody.Contains("/reset-password", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(match);
        return match!;
    }

    private static (Guid ChallengeId, string Token) ExtractResetChallenge(CapturedEmail email)
    {
        var match = ResetLinkRegex.Match(email.TextBody);
        Assert.True(match.Success, "Reset link missing from captured email text body.");
        return (Guid.Parse(match.Groups[1].Value), Uri.UnescapeDataString(match.Groups[2].Value));
    }

    private static TimeSpan ReadAccessTokenLifetime(string accessToken)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        return jwt.ValidTo.ToUniversalTime() - jwt.ValidFrom.ToUniversalTime();
    }

    private sealed class ForgotBody
    {
        public string Message { get; set; } = "";
    }

    private sealed class RegisterBody
    {
        public Guid UserId { get; set; }
    }

    private sealed class ProfileBody
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool EmailConfirmed { get; set; }
    }
}

/// <summary>
/// Password reset succeeds in Permixa, then host refresh-revocation throws.
/// Must not return normal { reset: true }.
/// </summary>
public sealed class PasswordResetRevocationFailureTests
    : IClassFixture<PasswordResetRevokeFailureWebApplicationFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Regex ResetLinkRegex = new(
        @"https?://[^\s]+/reset-password\?challengeId=([0-9a-fA-F-]{36})&token=([^\s]+)",
        RegexOptions.Compiled);

    private readonly PasswordResetRevokeFailureWebApplicationFactory _factory;

    public PasswordResetRevocationFailureTests(PasswordResetRevokeFailureWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Reset_WhenRevokeAllThrows_Returns503_PasswordChanged_RefreshStillUsable_ChallengeConsumed()
    {
        var client = _factory.CreateClient();
        _factory.Emails.Clear();
        _factory.RevokeFailureGate.FailRevokeAll = true;

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"revoke-fail-{suffix}@example.test";
        const string oldPassword = "Customer-Revoke-Fail-Old-1!";
        const string newPassword = "Customer-Revoke-Fail-New-2!";

        (await client.PostAsJsonAsync("/account/register", new
        {
            fullName = "Revoke Fail",
            email,
            password = oldPassword
        })).EnsureSuccessStatusCode();
        await _factory.ConfirmEmailFromOutboxAsync(client, email);
        _factory.Emails.Clear();

        var login = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = email,
            password = oldPassword
        });
        login.EnsureSuccessStatusCode();
        var before = (await login.Content.ReadFromJsonAsync<AuthTokenResponse>(Json))!;

        using var forgotRequest = new HttpRequestMessage(HttpMethod.Post, "/account/password/forgot")
        {
            Content = JsonContent.Create(new { email })
        };
        forgotRequest.Headers.TryAddWithoutValidation(
            ElectricalStore.Api.Hosting.TestConnectingIpStartupFilter.HeaderName,
            "203.0.113.200");
        (await client.SendAsync(forgotRequest)).EnsureSuccessStatusCode();

        var message = _factory.Emails.Sent.Last(e =>
            string.Equals(e.To, email, StringComparison.OrdinalIgnoreCase)
            && e.TextBody.Contains("/reset-password", StringComparison.OrdinalIgnoreCase));
        var match = ResetLinkRegex.Match(message.TextBody);
        Assert.True(match.Success);
        var challengeId = Guid.Parse(match.Groups[1].Value);
        var token = Uri.UnescapeDataString(match.Groups[2].Value);

        var reset = await client.PostAsJsonAsync("/account/password/reset", new
        {
            challengeId,
            token,
            newPassword
        });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, reset.StatusCode);
        using (var doc = JsonDocument.Parse(await reset.Content.ReadAsStringAsync()))
        {
            Assert.Equal(
                "Customer.PasswordResetSessionRevocationFailed",
                doc.RootElement.GetProperty("code").GetString());
            Assert.False(doc.RootElement.TryGetProperty("reset", out _));
        }

        // Partial state: password already changed; challenge consumed; refresh NOT revoked.
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/auth/login", new
            {
                emailOrUserName = email,
                password = oldPassword
            })).StatusCode);

        (await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = email,
            password = newPassword
        })).EnsureSuccessStatusCode();

        var refresh = await client.PostAsJsonAsync("/auth/refresh", new
        {
            refreshToken = before.RefreshToken
        });
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);

        var reuse = await client.PostAsJsonAsync("/account/password/reset", new
        {
            challengeId,
            token,
            newPassword = "Customer-Revoke-Fail-New-3!"
        });
        Assert.Equal(HttpStatusCode.Conflict, reuse.StatusCode);
    }
}

public sealed class PasswordResetRevokeFailureWebApplicationFactory : AppWebApplicationFactory
{
    public RevokeFailureGate RevokeFailureGate { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IRefreshTokenRepository>();
            services.AddSingleton(RevokeFailureGate);
            services.AddScoped<RefreshTokenRepository>();
            services.AddScoped<IRefreshTokenRepository>(sp =>
                new RevokeFailingRefreshTokenRepository(
                    sp.GetRequiredService<RefreshTokenRepository>(),
                    sp.GetRequiredService<RevokeFailureGate>()));
        });
    }
}
