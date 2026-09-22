namespace ElectricalStore.Domain.Customers;

/// <summary>
/// Store-owned customer profile linked 1:1 to a Permixa Identity user id.
/// Does not own login credentials (email/username/password remain in Permixa).
/// </summary>
public sealed class CustomerProfile
{
    public const int FullNameMaxLength = 200;

    /// <summary>Same as Permixa ApplicationUser.Id (external identity reference).</summary>
    public Guid UserId { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    private CustomerProfile()
    {
    }

    public static CustomerProfile Create(Guid userId, string fullName, DateTime utcNow)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required.", nameof(userId));

        var name = NormalizeFullName(fullName);
        return new CustomerProfile
        {
            UserId = userId,
            FullName = name,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void UpdateFullName(string fullName, DateTime utcNow)
    {
        FullName = NormalizeFullName(fullName);
        UpdatedAtUtc = utcNow;
    }

    public static string NormalizeFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));

        var trimmed = fullName.Trim();
        if (trimmed.Length > FullNameMaxLength)
            throw new ArgumentException(
                $"Full name must be {FullNameMaxLength} characters or fewer.",
                nameof(fullName));

        return trimmed;
    }
}
