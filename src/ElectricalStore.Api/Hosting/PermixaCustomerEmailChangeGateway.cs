using ElectricalStore.Application.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Permixa.Application.Authentication.Abstractions;
using Permixa.Application.Common.Abstractions;
using Permixa.Application.Identity.Abstractions;
using Permixa.Application.Verification.Abstractions;
using Permixa.Application.Verification.EmailChange;
using Permixa.Infrastructure.Email;
using Permixa.Infrastructure.Identity;

namespace ElectricalStore.Api.Hosting;

/// <summary>
/// Thin Permixa adapter for customer email change + refresh revocation + optional old-email notice.
/// </summary>
public sealed class PermixaCustomerEmailChangeGateway : ICustomerEmailChangeGateway
{
    public const string SessionRevocationFailedCode =
        "Customer.EmailChangeSessionRevocationFailed";

    private readonly IIdentityUserReader _users;
    private readonly IIdentityUserEmailReader _emails;
    private readonly IClock _clock;
    private readonly RequestEmailChangeUseCase _request;
    private readonly ConfirmEmailChangeUseCase _confirm;
    private readonly IVerificationChallengeRepository _challenges;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly EmailDeliveryHostOptions _emailOptions;
    private readonly ILogger<PermixaCustomerEmailChangeGateway> _logger;

    public PermixaCustomerEmailChangeGateway(
        IIdentityUserReader users,
        IIdentityUserEmailReader emails,
        IClock clock,
        RequestEmailChangeUseCase request,
        ConfirmEmailChangeUseCase confirm,
        IVerificationChallengeRepository challenges,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        IOptions<EmailDeliveryHostOptions> emailOptions,
        ILogger<PermixaCustomerEmailChangeGateway> logger)
    {
        _users = users;
        _emails = emails;
        _clock = clock;
        _request = request;
        _confirm = confirm;
        _challenges = challenges;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _emailSender = emailSender;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task<bool> IsDisabledAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var state = await _users.GetAccountStateAsync(userId, _clock.UtcNow, cancellationToken);
        return state?.IsDisabled == true;
    }

    public async Task<CustomerEmailChangeRequestOutcome> RequestAsync(
        Guid userId,
        string newEmail,
        string currentPassword,
        CancellationToken cancellationToken = default)
    {
        var result = await _request.ExecuteAsync(
            new RequestEmailChangeRequest
            {
                UserId = userId,
                NewEmail = newEmail,
                CurrentPassword = currentPassword
            },
            cancellationToken);

        if (!result.IsSuccess)
        {
            return new CustomerEmailChangeRequestOutcome(false, result.Error?.Code);
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        return new CustomerEmailChangeRequestOutcome(
            true,
            null,
            user?.PendingEmail);
    }

    public async Task<CustomerEmailChangeConfirmOutcome> ConfirmAsync(
        Guid challengeId,
        string token,
        CancellationToken cancellationToken = default)
    {
        var challenge = await _challenges.GetByIdAsync(challengeId, cancellationToken);
        var userId = challenge?.UserId;

        if (userId is Guid id && id != Guid.Empty)
        {
            if (await IsDisabledAsync(id, cancellationToken))
            {
                _logger.LogWarning(
                    "Email change confirmation rejected for disabled account {UserId}.",
                    id);
                return new CustomerEmailChangeConfirmOutcome(
                    false,
                    "Customer.EmailChangeUnavailable");
            }
        }

        string? previousEmail = null;
        string? technicalUserName = null;
        if (userId is Guid uid && uid != Guid.Empty)
        {
            var before = await _userManager.FindByIdAsync(uid.ToString());
            previousEmail = before?.Email;
            technicalUserName = before?.UserName;
        }

        var result = await _confirm.ExecuteAsync(
            new ConfirmEmailChangeRequest
            {
                ChallengeId = challengeId,
                Token = token
            },
            cancellationToken);

        if (!result.IsSuccess)
        {
            return new CustomerEmailChangeConfirmOutcome(false, result.Error?.Code);
        }

        if (userId is not Guid confirmedUserId || confirmedUserId == Guid.Empty)
        {
            _logger.LogError(
                "Email change confirmed for challenge {ChallengeId} but user id was missing; cannot revoke sessions.",
                challengeId);
            return new CustomerEmailChangeConfirmOutcome(false, SessionRevocationFailedCode);
        }

        var after = await _userManager.FindByIdAsync(confirmedUserId.ToString());
        var newEmail = after?.Email;

        // Technical UserName must remain stable (customer-{Guid}), not the email.
        if (!string.IsNullOrWhiteSpace(technicalUserName)
            && after is not null
            && !string.Equals(after.UserName, technicalUserName, StringComparison.Ordinal))
        {
            _logger.LogError(
                "Email change altered UserName for {UserId}; restoring technical username.",
                confirmedUserId);
            await _userManager.SetUserNameAsync(after, technicalUserName);
            await _userManager.UpdateNormalizedUserNameAsync(after);
        }

        try
        {
            await _refreshTokens.RevokeAllForUserAsync(
                confirmedUserId,
                _clock.UtcNow,
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Email was changed for {UserId} but refresh-token revocation failed. Challenge already consumed.",
                confirmedUserId);
            return new CustomerEmailChangeConfirmOutcome(
                false,
                SessionRevocationFailedCode,
                newEmail,
                previousEmail);
        }

        // Best-effort security notice to the previous address — never rolls back the change.
        if (!string.IsNullOrWhiteSpace(previousEmail)
            && !string.Equals(previousEmail, newEmail, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await SendOldEmailSecurityNoticeAsync(previousEmail!, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Email change succeeded for {UserId} but old-email security notice failed.",
                    confirmedUserId);
            }
        }

        return new CustomerEmailChangeConfirmOutcome(true, null, newEmail, previousEmail);
    }

    private Task SendOldEmailSecurityNoticeAsync(
        string previousEmail,
        CancellationToken cancellationToken)
    {
        var rendered = ArabicEmailChangeTemplates.RenderSecurityNotice(
            applicationName: string.IsNullOrWhiteSpace(_emailOptions.Branding.ApplicationName)
                ? "ElectricalStore"
                : _emailOptions.Branding.ApplicationName,
            companyName: _emailOptions.Branding.CompanyName,
            supportEmail: _emailOptions.Branding.SupportEmail,
            logoUrl: _emailOptions.Branding.LogoUrl);

        var fromEmail = string.IsNullOrWhiteSpace(_emailOptions.FromEmail)
            ? "noreply@localhost"
            : _emailOptions.FromEmail.Trim();

        return _emailSender.SendAsync(
            new EmailOutgoingMessage(
                To: previousEmail,
                From: fromEmail,
                Subject: rendered.Subject,
                HtmlBody: rendered.HtmlBody,
                TextBody: rendered.TextBody,
                IdempotencyKey: $"email-change-notice:{Guid.NewGuid():N}"),
            cancellationToken);
    }
}
