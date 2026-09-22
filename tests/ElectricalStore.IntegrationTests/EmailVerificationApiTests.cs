using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using ElectricalStore.IntegrationTests.Support;
using Xunit;

namespace ElectricalStore.IntegrationTests;

public sealed class EmailVerificationApiTests : IClassFixture<AppWebApplicationFactory>
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Regex VerificationLinkRegex = new(
        @"https?://[^\s]+/verify-email\?challengeId=([0-9a-fA-F-]{36})&token=([^\s]+)",
        RegexOptions.Compiled);

    private readonly AppWebApplicationFactory _factory;

    public EmailVerificationApiTests(AppWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_LoginBlocked_Confirm_LoginSucceeds_AndResendIsAntiEnumeration()
    {
        var client = _factory.CreateClient();
        _factory.Emails.Clear();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"verify-{suffix}@example.test";
        const string password = "Customer-Verify-Pass-1!";
        const string fullName = "عبدالهادي درويش";

        var register = await client.PostAsJsonAsync("/account/register", new
        {
            fullName,
            email,
            password
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var registered = (await register.Content.ReadFromJsonAsync<RegisterBody>(Json))!;
        Assert.True(registered.EmailVerificationRequired);
        Assert.True(registered.VerificationEmailSent);
        Assert.Equal(email, registered.Email);

        var blocked = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = email,
            password
        });
        Assert.Equal(HttpStatusCode.Unauthorized, blocked.StatusCode);
        using (var doc = JsonDocument.Parse(await blocked.Content.ReadAsStringAsync()))
        {
            Assert.Equal(
                "Authentication.EmailNotConfirmed",
                doc.RootElement.GetProperty("code").GetString());
            Assert.False(doc.RootElement.TryGetProperty("accessToken", out _));
        }

        var (challengeId, token) = ExtractChallenge(AwaitLatestEmail(email));
        var confirm = await client.PostAsJsonAsync("/account/email-verification/confirm", new
        {
            challengeId,
            token
        });
        confirm.EnsureSuccessStatusCode();

        var confirmAgain = await client.PostAsJsonAsync("/account/email-verification/confirm", new
        {
            challengeId,
            token
        });
        confirmAgain.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = email,
            password
        });
        login.EnsureSuccessStatusCode();
        var tokens = (await login.Content.ReadFromJsonAsync<AuthTokenResponse>(Json))!;
        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));

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

        var profile = await (await authed.GetAsync("/account/profile"))
            .Content.ReadFromJsonAsync<ProfileBody>(Json);
        Assert.Equal(fullName, profile!.FullName);
        Assert.True(profile.EmailConfirmed);

        // Anti-enumeration: unknown / confirmed emails look the same.
        _factory.Emails.Clear();
        var unknown = await client.PostAsJsonAsync("/account/email-verification/resend", new
        {
            email = $"missing-{suffix}@example.test"
        });
        unknown.EnsureSuccessStatusCode();
        Assert.Empty(_factory.Emails.Sent);

        var confirmedResend = await client.PostAsJsonAsync("/account/email-verification/resend", new
        {
            email
        });
        confirmedResend.EnsureSuccessStatusCode();
        Assert.Empty(_factory.Emails.Sent);
    }

    [Fact]
    public async Task Resend_Unconfirmed_DeliversNewMessage_AndInvalidTokenFailsSafely()
    {
        var client = _factory.CreateClient();
        _factory.Emails.Clear();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"resend-{suffix}@example.test";
        const string password = "Customer-Verify-Pass-2!";

        (await client.PostAsJsonAsync("/account/register", new
        {
            fullName = "Resend User",
            email,
            password
        })).EnsureSuccessStatusCode();

        Assert.NotEmpty(_factory.Emails.Sent);
        _factory.Emails.Clear();

        // Cooldown is 1s in this factory.
        await Task.Delay(1200);

        var resend = await client.PostAsJsonAsync("/account/email-verification/resend", new { email });
        resend.EnsureSuccessStatusCode();
        var (challengeId, token) = ExtractChallenge(AwaitLatestEmail(email));

        var bad = await client.PostAsJsonAsync("/account/email-verification/confirm", new
        {
            challengeId,
            token = "not-a-valid-token"
        });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        var good = await client.PostAsJsonAsync("/account/email-verification/confirm", new
        {
            challengeId,
            token
        });
        good.EnsureSuccessStatusCode();

        (await client.PostAsJsonAsync("/auth/login", new
        {
            emailOrUserName = email,
            password
        })).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Resend_RateLimit_Returns429_AfterRemoteIpPermitExhausted()
    {
        // Policy: EmailVerificationResend = 5 / 15 minutes / RemoteIp.
        var client = _factory.CreateClient();
        const string isolatedIp = "198.51.100.77";

        HttpResponseMessage? last = null;
        for (var i = 0; i < 6; i++)
        {
            last?.Dispose();
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/account/email-verification/resend")
            {
                Content = JsonContent.Create(new
                {
                    email = $"rl-{i}@example.test"
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

    private CapturedEmail AwaitLatestEmail(string to)
    {
        var match = _factory.Emails.Sent.LastOrDefault(e =>
            string.Equals(e.To, to, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(match);
        return match!;
    }

    private static (Guid ChallengeId, string Token) ExtractChallenge(CapturedEmail email)
    {
        var match = VerificationLinkRegex.Match(email.TextBody);
        Assert.True(match.Success, "Verification link missing from captured email text body.");
        return (Guid.Parse(match.Groups[1].Value), Uri.UnescapeDataString(match.Groups[2].Value));
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
