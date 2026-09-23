using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;
using Microsoft.Extensions.Logging;
using Permixa.Application.Authentication.Abstractions;
using Permixa.Application.Common.Abstractions;
using Permixa.Application.Identity.Abstractions;
using Permixa.Application.Verification.Abstractions;
using Permixa.Application.Verification.PasswordReset;

namespace ElectricalStore.Api.Hosting;

/// <summary>
/// Thin Permixa adapter for customer password reset + refresh-session revocation after success.
/// </summary>
public sealed class PermixaCustomerPasswordResetGateway : ICustomerPasswordResetGateway
{
    public const string SessionRevocationFailedCode =
        "Customer.PasswordResetSessionRevocationFailed";

    private readonly IIdentityUserEmailReader _emails;
    private readonly IIdentityUserReader _users;
    private readonly IClock _clock;
    private readonly RequestPasswordResetUseCase _requestReset;
    private readonly ResetPasswordWithVerificationUseCase _resetPassword;
    private readonly IVerificationChallengeRepository _challenges;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PermixaCustomerPasswordResetGateway> _logger;

    public PermixaCustomerPasswordResetGateway(
        IIdentityUserEmailReader emails,
        IIdentityUserReader users,
        IClock clock,
        RequestPasswordResetUseCase requestReset,
        ResetPasswordWithVerificationUseCase resetPassword,
        IVerificationChallengeRepository challenges,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        ILogger<PermixaCustomerPasswordResetGateway> logger)
    {
        _emails = emails;
        _users = users;
        _clock = clock;
        _requestReset = requestReset;
        _resetPassword = resetPassword;
        _challenges = challenges;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Task<Guid?> FindUserIdByEmailAsync(
        string email,
        CancellationToken cancellationToken = default) =>
        _emails.FindUserIdByEmailAsync(email, cancellationToken);

    public async Task<bool> IsDisabledAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var state = await _users.GetAccountStateAsync(userId, _clock.UtcNow, cancellationToken);
        return state?.IsDisabled == true;
    }

    public async Task TryRequestResetEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        // Permixa RequestPasswordResetUseCase is already anti-enumeration (always Success).
        var result = await _requestReset.ExecuteAsync(
            new RequestPasswordResetRequest { Email = email },
            cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "Password reset request completed without Success status: {Code}",
                result.Error?.Code);
        }
    }

    public async Task<CustomerPasswordResetOutcome> ResetPasswordAsync(
        Guid challengeId,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var challenge = await _challenges.GetByIdAsync(challengeId, cancellationToken);
        var userId = challenge?.UserId;

        // Challenge issued while eligible, then account disabled: do not complete reset
        // (challenge remains unused so it can be invalidated after re-enable + new request).
        if (userId is Guid disabledCheckId && disabledCheckId != Guid.Empty)
        {
            if (await IsDisabledAsync(disabledCheckId, cancellationToken))
            {
                _logger.LogWarning(
                    "Password reset rejected for disabled account {UserId}.",
                    disabledCheckId);
                return new CustomerPasswordResetOutcome(false, "Customer.PasswordResetFailed");
            }
        }

        var result = await _resetPassword.ExecuteAsync(
            new ResetPasswordWithVerificationRequest
            {
                ChallengeId = challengeId,
                Token = token,
                NewPassword = newPassword
            },
            cancellationToken);

        if (!result.IsSuccess)
        {
            return new CustomerPasswordResetOutcome(false, result.Error?.Code);
        }

        // Challenge is consumed by Permixa at this point. Password may already be updated.
        // Permixa self-service reset does not revoke refresh families; host closes the gap.
        if (userId is not Guid id || id == Guid.Empty)
        {
            _logger.LogError(
                "Password reset succeeded for challenge {ChallengeId} but user id was missing; cannot revoke refresh sessions.",
                challengeId);
            return new CustomerPasswordResetOutcome(false, SessionRevocationFailedCode);
        }

        try
        {
            await _refreshTokens.RevokeAllForUserAsync(id, _clock.UtcNow, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Must not report normal success while renewable sessions may still work.
            _logger.LogError(
                ex,
                "Password was reset for {UserId} but refresh-token revocation failed. Challenge already consumed.",
                id);
            return new CustomerPasswordResetOutcome(false, SessionRevocationFailedCode);
        }

        return new CustomerPasswordResetOutcome(true, null);
    }
}
