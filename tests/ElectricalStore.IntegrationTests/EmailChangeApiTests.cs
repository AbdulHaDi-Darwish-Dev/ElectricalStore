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

public sealed class EmailChangeApiTests : IClassFixture<AppWebApplicationFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Regex ChangeLinkRegex = new(
        @"https?://[^\s]+/change-email/confirm\?challengeId=([0-9a-fA-F-]{36})&token=([^\s]+)",
        RegexOptions.Compiled);

    private readonly AppWebApplicationFactory _factory;

    public EmailChangeApiTests(AppWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HappyPath_PendingUntilConfirm_UserNameStable_RefreshRevoked_OldLoginFails()
    {
        var client = _factory.CreateClient();
        _factory.Emails.Clear();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var oldEmail = $"old-ec-{suffix}@example.test";
        var newEmail = $"new-ec-{suffix}@example.test";
        const string password = "Customer-EmailChange-1!";
        const string fullName = "عبدالهادي درويش";

        var register = await client.PostAsJsonAsync("/account/register", new
        {
            fullName,
            email = oldEmail,
            password
        });
        register.EnsureSuccessStatusCode();
        var registered = (await register.Content.ReadFromJsonAsync<RegisterBody>(Json))!;
        await _factory.ConfirmEmailFromOutboxAsync(client, oldEmail);
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
            emailOrUserName = oldEmail,
            password
        });
        loginA.EnsureSuccessStatusCode();
        var sessionA = (await loginA.Content.ReadFromJsonAsync<AuthTokenResponse>(Json))!;

        var loginB = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = oldEmail,
            password
        });
        loginB.EnsureSuccessStatusCode();
        var sessionB = (await loginB.Content.ReadFromJsonAsync<AuthTokenResponse>(Json))!;
        Assert.NotEqual(sessionA.RefreshToken, sessionB.RefreshToken);

        var accessLifetime = ReadAccessTokenLifetime(sessionA.AccessToken);
        Assert.True(
            accessLifetime >= TimeSpan.FromMinutes(10) && accessLifetime <= TimeSpan.FromHours(2),
            $"Unexpected access JWT lifetime: {accessLifetime}");

        var authed = _factory.CreateAuthenticatedClient(sessionA.AccessToken);
        var request = await authed.PostAsJsonAsync("/account/email-change/request", new
        {
            newEmail,
            currentPassword = password
        });
        request.EnsureSuccessStatusCode();
        var requestBody = (await request.Content.ReadFromJsonAsync<RequestBody>(Json))!;
        Assert.Equal(
            ElectricalStore.Application.Customers.RequestCustomerEmailChangeUseCase.SuccessMessage,
            requestBody.Message);
        Assert.Equal(newEmail, requestBody.PendingEmail, ignoreCase: true);

        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(registered.UserId.ToString());
            Assert.NotNull(user);
            Assert.Equal(oldEmail, user!.Email, ignoreCase: true);
            Assert.Equal(newEmail, user.PendingEmail, ignoreCase: true);
            Assert.True(user.EmailConfirmed);
            Assert.Equal(technicalUserName, user.UserName);
        }

        var profilePending = await (await authed.GetAsync("/account/profile"))
            .Content.ReadFromJsonAsync<ProfileBody>(Json);
        Assert.Equal(oldEmail, profilePending!.Email, ignoreCase: true);
        Assert.Equal(fullName, profilePending.FullName);

        // Old email still logs in before confirmation.
        (await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = oldEmail,
            password
        })).EnsureSuccessStatusCode();

        var changeMail = AwaitLatestChangeEmail(newEmail);
        Assert.Contains("تأكيد تغيير البريد", changeMail.Subject, StringComparison.Ordinal);
        var (challengeId, token) = ExtractChangeChallenge(changeMail);

        var confirm = await client.PostAsJsonAsync("/account/email-change/confirm", new
        {
            challengeId,
            token
        });
        confirm.EnsureSuccessStatusCode();
        using (var doc = JsonDocument.Parse(await confirm.Content.ReadAsStringAsync()))
        {
            Assert.True(doc.RootElement.GetProperty("changed").GetBoolean());
            Assert.Equal(
                newEmail,
                doc.RootElement.GetProperty("email").GetString(),
                ignoreCase: true);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(registered.UserId.ToString());
            Assert.NotNull(user);
            Assert.Equal(newEmail, user!.Email, ignoreCase: true);
            Assert.True(string.IsNullOrEmpty(user.PendingEmail));
            Assert.True(user.EmailConfirmed);
            Assert.Equal(technicalUserName, user.UserName);
        }

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/auth/login", new
            {
                emailOrUserName = oldEmail,
                password
            })).StatusCode);

        var newLogin = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = newEmail,
            password
        });
        newLogin.EnsureSuccessStatusCode();

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = sessionA.RefreshToken }))
                .StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = sessionB.RefreshToken }))
                .StatusCode);

        // Stateless access JWT remains valid until expiry.
        Assert.Equal(HttpStatusCode.OK, (await _factory.CreateAuthenticatedClient(sessionA.AccessToken)
            .GetAsync("/me")).StatusCode);

        var profile = await (await _factory.CreateAuthenticatedClient(
                (await newLogin.Content.ReadFromJsonAsync<AuthTokenResponse>(Json))!.AccessToken)
            .GetAsync("/account/profile")).Content.ReadFromJsonAsync<ProfileBody>(Json);
        Assert.Equal(fullName, profile!.FullName);
        Assert.Equal(newEmail, profile.Email, ignoreCase: true);
        Assert.True(profile.EmailConfirmed);

        // Old-email security notice (no token).
        var notice = _factory.Emails.Sent.LastOrDefault(e =>
            string.Equals(e.To, oldEmail, StringComparison.OrdinalIgnoreCase)
            && e.Subject.Contains("تغيير البريد", StringComparison.Ordinal));
        Assert.NotNull(notice);
        Assert.DoesNotContain("challengeId=", notice!.TextBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token=", notice.TextBody, StringComparison.OrdinalIgnoreCase);

        // Token reuse blocked.
        var reuse = await client.PostAsJsonAsync("/account/email-change/confirm", new
        {
            challengeId,
            token
        });
        Assert.Equal(HttpStatusCode.Conflict, reuse.StatusCode);
    }

    [Fact]
    public async Task Request_WrongPassword_NoEmail_ActiveUnchanged()
    {
        var (authed, oldEmail, password, userId) = await RegisterLoginAsync("wrongpw");
        _factory.Emails.Clear();
        var newEmail = $"new-wrongpw-{Guid.NewGuid().ToString("N")[..8]}@example.test";

        var response = await authed.PostAsJsonAsync("/account/email-change/request", new
        {
            newEmail,
            currentPassword = "Definitely-Wrong-Pass-9!"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using (var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
        {
            Assert.Equal(
                "Customer.CurrentPasswordInvalid",
                doc.RootElement.GetProperty("code").GetString());
        }

        Assert.DoesNotContain(
            _factory.Emails.Sent,
            e => e.TextBody.Contains("/change-email/confirm", StringComparison.OrdinalIgnoreCase));

        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByIdAsync(userId.ToString());
        Assert.Equal(oldEmail, user!.Email, ignoreCase: true);
        Assert.True(string.IsNullOrEmpty(user.PendingEmail));
        Assert.False(string.IsNullOrEmpty(password));
    }

    [Fact]
    public async Task Request_DuplicateTarget_Fails()
    {
        var client = _factory.CreateClient();
        _factory.Emails.Clear();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var occupied = $"taken-ec-{suffix}@example.test";
        var actor = $"actor-ec-{suffix}@example.test";
        const string password = "Customer-EmailChange-Dup-1!";

        (await client.PostAsJsonAsync("/account/register", new
        {
            fullName = "Occupied",
            email = occupied,
            password
        })).EnsureSuccessStatusCode();
        await _factory.ConfirmEmailFromOutboxAsync(client, occupied);

        (await client.PostAsJsonAsync("/account/register", new
        {
            fullName = "Actor",
            email = actor,
            password
        })).EnsureSuccessStatusCode();
        await _factory.ConfirmEmailFromOutboxAsync(client, actor);
        _factory.Emails.Clear();

        var login = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = actor,
            password
        });
        login.EnsureSuccessStatusCode();
        var tokens = (await login.Content.ReadFromJsonAsync<AuthTokenResponse>(Json))!;
        var authed = _factory.CreateAuthenticatedClient(tokens.AccessToken);

        var response = await authed.PostAsJsonAsync("/account/email-change/request", new
        {
            newEmail = occupied,
            currentPassword = password
        });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.DoesNotContain(
            _factory.Emails.Sent,
            e => e.TextBody.Contains("/change-email/confirm", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Request_NormalizedSameEmail_FailsUnchanged()
    {
        var (authed, oldEmail, password, _) = await RegisterLoginAsync("samecase");
        _factory.Emails.Clear();

        var mixed = char.IsUpper(oldEmail[0])
            ? oldEmail.ToLowerInvariant()
            : char.ToUpperInvariant(oldEmail[0]) + oldEmail[1..];

        var response = await authed.PostAsJsonAsync("/account/email-change/request", new
        {
            newEmail = mixed,
            currentPassword = password
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Customer.EmailUnchanged", doc.RootElement.GetProperty("code").GetString());
        Assert.Empty(_factory.Emails.Sent);
    }

    [Fact]
    public async Task Confirm_InvalidToken_BadRequest()
    {
        var (authed, _, password, _) = await RegisterLoginAsync("badtok");
        var newEmail = $"new-badtok-{Guid.NewGuid().ToString("N")[..8]}@example.test";
        _factory.Emails.Clear();

        (await authed.PostAsJsonAsync("/account/email-change/request", new
        {
            newEmail,
            currentPassword = password
        })).EnsureSuccessStatusCode();

        var (challengeId, _) = ExtractChangeChallenge(AwaitLatestChangeEmail(newEmail));
        var bad = await _factory.CreateClient().PostAsJsonAsync("/account/email-change/confirm", new
        {
            challengeId,
            token = "not-a-valid-token"
        });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task Confirm_DisabledAfterRequest_Rejects_KeepsDisabled_EmailUnchanged()
    {
        var (authed, oldEmail, password, userId) = await RegisterLoginAsync("disafter");
        var newEmail = $"new-disafter-{Guid.NewGuid().ToString("N")[..8]}@example.test";
        _factory.Emails.Clear();

        (await authed.PostAsJsonAsync("/account/email-change/request", new
        {
            newEmail,
            currentPassword = password
        })).EnsureSuccessStatusCode();
        var (challengeId, token) = ExtractChangeChallenge(AwaitLatestChangeEmail(newEmail));

        using (var scope = _factory.Services.CreateScope())
        {
            var writer = scope.ServiceProvider.GetRequiredService<IIdentityUserWriter>();
            var uow = scope.ServiceProvider.GetRequiredService<Permixa.Application.Common.Abstractions.IUnitOfWork>();
            await writer.SetDisabledAsync(userId, true);
            await uow.SaveChangesAsync();
        }

        var confirm = await _factory.CreateClient().PostAsJsonAsync("/account/email-change/confirm", new
        {
            challengeId,
            token
        });
        Assert.Equal(HttpStatusCode.Forbidden, confirm.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var readers = scope.ServiceProvider.GetRequiredService<IIdentityUserReader>();
            var clock = scope.ServiceProvider.GetRequiredService<Permixa.Application.Common.Abstractions.IClock>();
            var user = await users.FindByIdAsync(userId.ToString());
            Assert.Equal(oldEmail, user!.Email, ignoreCase: true);
            var state = await readers.GetAccountStateAsync(userId, clock.UtcNow);
            Assert.True(state!.IsDisabled);
        }
    }

    [Fact]
    public async Task CrossPurpose_PasswordResetToken_CannotConfirmEmailChange()
    {
        var client = _factory.CreateClient();
        _factory.Emails.Clear();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"xpurpose-{suffix}@example.test";
        const string password = "Customer-CrossPurpose-1!";

        (await client.PostAsJsonAsync("/account/register", new
        {
            fullName = "Cross Purpose",
            email,
            password
        })).EnsureSuccessStatusCode();
        await _factory.ConfirmEmailFromOutboxAsync(client, email);
        _factory.Emails.Clear();

        using var forgotRequest = new HttpRequestMessage(HttpMethod.Post, "/account/password/forgot")
        {
            Content = JsonContent.Create(new { email })
        };
        forgotRequest.Headers.TryAddWithoutValidation(
            ElectricalStore.Api.Hosting.TestConnectingIpStartupFilter.HeaderName,
            $"203.0.113.{Random.Shared.Next(1, 254)}");
        (await client.SendAsync(forgotRequest)).EnsureSuccessStatusCode();

        var resetMail = _factory.Emails.Sent.Last(e =>
            string.Equals(e.To, email, StringComparison.OrdinalIgnoreCase)
            && e.TextBody.Contains("/reset-password", StringComparison.OrdinalIgnoreCase));
        var resetMatch = new Regex(
            @"challengeId=([0-9a-fA-F-]{36})&token=([^\s]+)",
            RegexOptions.Compiled).Match(resetMail.TextBody);
        Assert.True(resetMatch.Success);
        var challengeId = Guid.Parse(resetMatch.Groups[1].Value);
        var token = Uri.UnescapeDataString(resetMatch.Groups[2].Value);

        var confirm = await client.PostAsJsonAsync("/account/email-change/confirm", new
        {
            challengeId,
            token
        });
        Assert.Equal(HttpStatusCode.BadRequest, confirm.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        Assert.Equal(email, user!.Email, ignoreCase: true);
    }

    [Fact]
    public async Task CrossPurpose_EmailChangeToken_CannotResetPassword()
    {
        var (authed, oldEmail, password, _) = await RegisterLoginAsync("xec2pw");
        var newEmail = $"xec2pw-new-{Guid.NewGuid().ToString("N")[..8]}@example.test";
        _factory.Emails.Clear();

        (await authed.PostAsJsonAsync("/account/email-change/request", new
        {
            newEmail,
            currentPassword = password
        })).EnsureSuccessStatusCode();
        var (challengeId, token) = ExtractChangeChallenge(AwaitLatestChangeEmail(newEmail));

        var reset = await _factory.CreateClient().PostAsJsonAsync("/account/password/reset", new
        {
            challengeId,
            token,
            newPassword = "Customer-CrossPurpose-New-9!"
        });
        Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);

        // Password and active email unchanged.
        (await _factory.CreateClient().PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = oldEmail,
            password
        })).EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(oldEmail);
        Assert.Equal(oldEmail, user!.Email, ignoreCase: true);
    }

    [Fact]
    public async Task CrossPurpose_EmailConfirmationToken_CannotConfirmEmailChange()
    {
        var client = _factory.CreateClient();
        _factory.Emails.Clear();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"xconf-ec-{suffix}@example.test";
        const string password = "Customer-CrossConf-Ec-1!";

        (await client.PostAsJsonAsync("/account/register", new
        {
            fullName = "Cross Conf EC",
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

        var confirm = await client.PostAsJsonAsync("/account/email-change/confirm", new
        {
            challengeId,
            token
        });
        Assert.Equal(HttpStatusCode.BadRequest, confirm.StatusCode);

        // Original verification token still usable for its purpose.
        (await client.PostAsJsonAsync("/account/email-verification/confirm", new
        {
            challengeId,
            token
        })).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task MultiRequest_LaterChallengeWins_EarlierTokenFailsSafely()
    {
        var (authed, oldEmail, password, userId) = await RegisterLoginAsync("multi");
        var emailA = $"new-a-{Guid.NewGuid().ToString("N")[..8]}@example.test";
        var emailB = $"new-b-{Guid.NewGuid().ToString("N")[..8]}@example.test";
        _factory.Emails.Clear();

        (await authed.PostAsJsonAsync("/account/email-change/request", new
        {
            newEmail = emailA,
            currentPassword = password
        })).EnsureSuccessStatusCode();
        var first = ExtractChangeChallenge(AwaitLatestChangeEmail(emailA));

        // Allow cooldown if any (factory may use short resend).
        await Task.Delay(1100);

        (await authed.PostAsJsonAsync("/account/email-change/request", new
        {
            newEmail = emailB,
            currentPassword = password
        })).EnsureSuccessStatusCode();
        var second = ExtractChangeChallenge(AwaitLatestChangeEmail(emailB));

        var stale = await _factory.CreateClient().PostAsJsonAsync("/account/email-change/confirm", new
        {
            challengeId = first.ChallengeId,
            token = first.Token
        });
        // Stale challenge should fail; active email remains old until a valid confirm.
        Assert.True(
            stale.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict,
            $"Unexpected stale confirm status: {stale.StatusCode}");

        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(userId.ToString());
            Assert.Equal(oldEmail, user!.Email, ignoreCase: true);
        }

        var ok = await _factory.CreateClient().PostAsJsonAsync("/account/email-change/confirm", new
        {
            challengeId = second.ChallengeId,
            token = second.Token
        });
        ok.EnsureSuccessStatusCode();

        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(userId.ToString());
            Assert.Equal(emailB, user!.Email, ignoreCase: true);
        }
    }

    [Fact]
    public async Task Confirm_TargetOccupiedBeforeConfirm_FailsSafely()
    {
        var client = _factory.CreateClient();
        _factory.Emails.Clear();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var actorEmail = $"race-actor-{suffix}@example.test";
        var targetEmail = $"race-target-{suffix}@example.test";
        const string password = "Customer-EmailChange-Race-1!";

        (await client.PostAsJsonAsync("/account/register", new
        {
            fullName = "Race Actor",
            email = actorEmail,
            password
        })).EnsureSuccessStatusCode();
        await _factory.ConfirmEmailFromOutboxAsync(client, actorEmail);
        _factory.Emails.Clear();

        var login = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = actorEmail,
            password
        });
        var tokens = (await login.Content.ReadFromJsonAsync<AuthTokenResponse>(Json))!;
        var authed = _factory.CreateAuthenticatedClient(tokens.AccessToken);

        (await authed.PostAsJsonAsync("/account/email-change/request", new
        {
            newEmail = targetEmail,
            currentPassword = password
        })).EnsureSuccessStatusCode();
        var (challengeId, token) = ExtractChangeChallenge(AwaitLatestChangeEmail(targetEmail));

        // Occupy target before confirm.
        (await client.PostAsJsonAsync("/account/register", new
        {
            fullName = "Race Occupier",
            email = targetEmail,
            password
        })).EnsureSuccessStatusCode();
        await _factory.ConfirmEmailFromOutboxAsync(client, targetEmail);

        var confirm = await client.PostAsJsonAsync("/account/email-change/confirm", new
        {
            challengeId,
            token
        });
        Assert.True(
            confirm.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest,
            $"Unexpected race confirm status: {confirm.StatusCode}");

        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var actor = await users.FindByEmailAsync(actorEmail);
        Assert.NotNull(actor);
        Assert.Equal(actorEmail, actor!.Email, ignoreCase: true);
    }

    private async Task<(HttpClient Authed, string OldEmail, string Password, Guid UserId)> RegisterLoginAsync(
        string label)
    {
        var client = _factory.CreateClient();
        _factory.Emails.Clear();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"{label}-{suffix}@example.test";
        const string password = "Customer-EmailChange-1!";

        var register = await client.PostAsJsonAsync("/account/register", new
        {
            fullName = label,
            email,
            password
        });
        register.EnsureSuccessStatusCode();
        var registered = (await register.Content.ReadFromJsonAsync<RegisterBody>(Json))!;
        await _factory.ConfirmEmailFromOutboxAsync(client, email);

        var login = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = email,
            password
        });
        login.EnsureSuccessStatusCode();
        var tokens = (await login.Content.ReadFromJsonAsync<AuthTokenResponse>(Json))!;
        return (_factory.CreateAuthenticatedClient(tokens.AccessToken), email, password, registered.UserId);
    }

    private CapturedEmail AwaitLatestChangeEmail(string to)
    {
        var match = _factory.Emails.Sent.LastOrDefault(e =>
            string.Equals(e.To, to, StringComparison.OrdinalIgnoreCase)
            && e.TextBody.Contains("/change-email/confirm", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(match);
        return match!;
    }

    private static (Guid ChallengeId, string Token) ExtractChangeChallenge(CapturedEmail email)
    {
        var match = ChangeLinkRegex.Match(email.TextBody);
        Assert.True(match.Success, "Change-email link missing from captured email text body.");
        return (Guid.Parse(match.Groups[1].Value), Uri.UnescapeDataString(match.Groups[2].Value));
    }

    private static TimeSpan ReadAccessTokenLifetime(string accessToken)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        return jwt.ValidTo.ToUniversalTime() - jwt.ValidFrom.ToUniversalTime();
    }

    private sealed class RequestBody
    {
        public string Message { get; set; } = "";
        public string? PendingEmail { get; set; }
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

public sealed class EmailChangeRevocationFailureTests
    : IClassFixture<EmailChangeRevokeFailureWebApplicationFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Regex ChangeLinkRegex = new(
        @"https?://[^\s]+/change-email/confirm\?challengeId=([0-9a-fA-F-]{36})&token=([^\s]+)",
        RegexOptions.Compiled);

    private readonly EmailChangeRevokeFailureWebApplicationFactory _factory;

    public EmailChangeRevocationFailureTests(EmailChangeRevokeFailureWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Confirm_WhenRevokeAllThrows_Returns503_EmailChanged_RefreshStillUsable()
    {
        var client = _factory.CreateClient();
        _factory.Emails.Clear();
        _factory.RevokeFailureGate.FailRevokeAll = true;

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var oldEmail = $"ec-revoke-old-{suffix}@example.test";
        var newEmail = $"ec-revoke-new-{suffix}@example.test";
        const string password = "Customer-Ec-Revoke-Fail-1!";

        (await client.PostAsJsonAsync("/account/register", new
        {
            fullName = "Ec Revoke Fail",
            email = oldEmail,
            password
        })).EnsureSuccessStatusCode();
        await _factory.ConfirmEmailFromOutboxAsync(client, oldEmail);
        _factory.Emails.Clear();

        var login = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = oldEmail,
            password
        });
        login.EnsureSuccessStatusCode();
        var before = (await login.Content.ReadFromJsonAsync<AuthTokenResponse>(Json))!;
        var authed = _factory.CreateAuthenticatedClient(before.AccessToken);

        (await authed.PostAsJsonAsync("/account/email-change/request", new
        {
            newEmail,
            currentPassword = password
        })).EnsureSuccessStatusCode();

        var message = _factory.Emails.Sent.Last(e =>
            string.Equals(e.To, newEmail, StringComparison.OrdinalIgnoreCase)
            && e.TextBody.Contains("/change-email/confirm", StringComparison.OrdinalIgnoreCase));
        var match = ChangeLinkRegex.Match(message.TextBody);
        Assert.True(match.Success);
        var challengeId = Guid.Parse(match.Groups[1].Value);
        var token = Uri.UnescapeDataString(match.Groups[2].Value);

        var confirm = await client.PostAsJsonAsync("/account/email-change/confirm", new
        {
            challengeId,
            token
        });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, confirm.StatusCode);
        using (var doc = JsonDocument.Parse(await confirm.Content.ReadAsStringAsync()))
        {
            Assert.Equal(
                "Customer.EmailChangeSessionRevocationFailed",
                doc.RootElement.GetProperty("code").GetString());
            Assert.False(doc.RootElement.TryGetProperty("changed", out _));
        }

        // Partial state: email already changed; refresh NOT revoked.
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/auth/login", new
            {
                emailOrUserName = oldEmail,
                password
            })).StatusCode);

        (await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = newEmail,
            password
        })).EnsureSuccessStatusCode();

        var refresh = await client.PostAsJsonAsync("/auth/refresh", new
        {
            refreshToken = before.RefreshToken
        });
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);

        var reuse = await client.PostAsJsonAsync("/account/email-change/confirm", new
        {
            challengeId,
            token
        });
        Assert.Equal(HttpStatusCode.Conflict, reuse.StatusCode);
    }
}

public sealed class EmailChangeRevokeFailureWebApplicationFactory : AppWebApplicationFactory
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
