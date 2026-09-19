using ElectricalStore.Domain.Ordering;

namespace ElectricalStore.Application.Abstractions;

public sealed record OrderCatalogLine(
    Guid ProductVariantId,
    Guid ProductId,
    string ProductName,
    string? PrimaryImageUrl,
    string VariantName,
    string Sku,
    string SellingUnit,
    decimal QuantityIncrement,
    decimal UnitPrice,
    bool ProductIsActive,
    bool VariantIsActive,
    bool CategoryIsActive,
    bool CategoryHasImage,
    bool ProductHasImage)
{
    public bool IsCatalogReady =>
        ProductIsActive && VariantIsActive && CategoryIsActive && CategoryHasImage && ProductHasImage;
}

public interface IOrderCatalogQuery
{
    Task<IReadOnlyDictionary<Guid, OrderCatalogLine>> GetCatalogLinesAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken = default);
}

public interface IGuestOrderTokenService
{
    (string RawToken, string TokenHash) CreateToken();

    string HashToken(string rawToken);

    bool TokensMatch(string rawToken, string storedHash);
}

/// <summary>
/// Protects guest order bearer tokens for short-lived idempotency replay.
/// Implementation lives in Infrastructure (e.g. ASP.NET Data Protection).
/// </summary>
public interface IGuestAccessTokenProtector
{
    string Protect(string rawGuestAccessToken);

    /// <summary>Returns null when the payload is invalid or cannot be unprotected.</summary>
    string? Unprotect(string protectedGuestAccessToken);
}

public sealed class OrderConcurrencyConflictException : Exception
{
    public OrderConcurrencyConflictException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed record OrderAdminListFilter(
    OrderStatus? Status = null,
    PaymentStatus? PaymentStatus = null,
    string? Search = null,
    DateTime? CreatedFromUtc = null,
    DateTime? CreatedToUtc = null);

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Order?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> ListAdminAsync(
        OrderAdminListFilter filter,
        CancellationToken cancellationToken = default);

    Task<bool> OrderNumberExistsAsync(string orderNumber, CancellationToken cancellationToken = default);
}

public interface IOrderingSettingsRepository
{
    /// <summary>Returns the singleton settings row, creating default (0) if missing.</summary>
    Task<OrderingSettings> GetOrCreateAsync(CancellationToken cancellationToken = default);

    Task<OrderingSettings> GetTrackedOrCreateAsync(CancellationToken cancellationToken = default);
}

public interface IOrderPlacementIdempotencyRepository
{
    Task<OrderPlacementIdempotency?> FindAsync(
        string scope,
        string keyHash,
        CancellationToken cancellationToken = default);

    Task AddAsync(OrderPlacementIdempotency record, CancellationToken cancellationToken = default);

    Task RemoveAsync(OrderPlacementIdempotency record, CancellationToken cancellationToken = default);
}

