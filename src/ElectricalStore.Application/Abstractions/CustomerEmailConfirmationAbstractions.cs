namespace ElectricalStore.Application.Abstractions;

/// <summary>
/// Host/Permixa adapter for customer email confirmation issuance and lookup.
/// Application owns eligibility/anti-enumeration policy; this abstraction owns Identity delivery.
/// </summary>
public interface ICustomerEmailConfirmationGateway
{
    Task<Guid?> FindUserIdByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> IsEmailConfirmedAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests confirmation delivery for an identity user.
    /// Does not throw for provider/use-case failures — returns a structured outcome.
    /// </summary>
    Task<CustomerEmailConfirmationIssueOutcome> TryRequestConfirmationAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed record CustomerEmailConfirmationIssueOutcome(
    bool Sent,
    bool AlreadyConfirmed,
    string? ErrorCode);
