using ElectricalStore.Domain.Customers;

namespace ElectricalStore.Application.Abstractions;

public interface ICustomerProfileRepository
{
    Task<CustomerProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(CustomerProfile profile, CancellationToken cancellationToken = default);
}

/// <summary>
/// Read-only identity snapshot from Permixa/Identity (email confirmation, etc.).
/// </summary>
public sealed record CustomerIdentityInfo(
    Guid UserId,
    string Email,
    bool EmailConfirmed,
    string UserName);

public interface ICustomerIdentityLookup
{
    Task<CustomerIdentityInfo?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Compensating cleanup when CustomerProfile persistence fails after identity registration.
/// Separate DbContexts — not a distributed transaction.
/// </summary>
public interface ICustomerIdentityCompensation
{
    /// <summary>Attempts to remove a just-created identity user. Returns false if cleanup failed.</summary>
    Task<bool> TryDeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Server-owned technical UserName for Identity. Uses only AllowedUserNameCharacters-safe chars.
/// Not shown to customers; login continues via email.
/// </summary>
public static class CustomerTechnicalUserName
{
    /// <summary>Format: customer-{32 hex digits}. Alphabet: a-z, 0-9, hyphen.</summary>
    public static string Create() => $"customer-{Guid.NewGuid():N}";
}
