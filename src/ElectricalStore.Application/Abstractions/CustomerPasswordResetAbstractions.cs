namespace ElectricalStore.Application.Abstractions;

/// <summary>
/// Host adapter for Permixa password-reset issuance/completion and session revocation.
/// </summary>
public interface ICustomerPasswordResetGateway
{
    Task<Guid?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> IsDisabledAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a password-reset challenge and dispatches email when possible.
    /// Must not throw distinguishing errors for anti-enumeration callers.
    /// </summary>
    Task TryRequestResetEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes password reset via Permixa challenge+token and revokes refresh sessions on success.
    /// </summary>
    Task<CustomerPasswordResetOutcome> ResetPasswordAsync(
        Guid challengeId,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);
}

public sealed record CustomerPasswordResetOutcome(bool Succeeded, string? ErrorCode);
