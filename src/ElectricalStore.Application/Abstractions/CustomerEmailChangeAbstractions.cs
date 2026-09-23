namespace ElectricalStore.Application.Abstractions;

/// <summary>
/// Host adapter for Permixa email-change request/confirm and refresh revocation.
/// </summary>
public interface ICustomerEmailChangeGateway
{
    Task<bool> IsDisabledAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<CustomerEmailChangeRequestOutcome> RequestAsync(
        Guid userId,
        string newEmail,
        string currentPassword,
        CancellationToken cancellationToken = default);

    Task<CustomerEmailChangeConfirmOutcome> ConfirmAsync(
        Guid challengeId,
        string token,
        CancellationToken cancellationToken = default);
}

public sealed record CustomerEmailChangeRequestOutcome(
    bool Succeeded,
    string? ErrorCode,
    string? PendingEmail = null);

public sealed record CustomerEmailChangeConfirmOutcome(
    bool Succeeded,
    string? ErrorCode,
    string? NewEmail = null,
    string? PreviousEmail = null);
