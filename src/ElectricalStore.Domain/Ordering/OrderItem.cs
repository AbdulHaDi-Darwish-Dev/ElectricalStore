namespace ElectricalStore.Domain.Ordering;

/// <summary>Historical sale line. Snapshots catalog fields at Place Order (or Pending reprice).</summary>
public sealed class OrderItem
{
    public const int NameMaxLength = 200;
    public const int SkuMaxLength = 64;
    public const int SellingUnitMaxLength = 16;

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid ProductId { get; private set; }

    public Guid ProductVariantId { get; private set; }

    public string ProductName { get; private set; } = string.Empty;

    public string VariantName { get; private set; } = string.Empty;

    public string Sku { get; private set; } = string.Empty;

    public string SellingUnit { get; private set; } = string.Empty;

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineTotal { get; private set; }

    private OrderItem()
    {
    }

    public static OrderItem Create(
        Guid orderId,
        Guid productId,
        Guid productVariantId,
        string productName,
        string variantName,
        string sku,
        string sellingUnit,
        decimal quantity,
        decimal unitPrice,
        decimal lineTotal)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("Order id is required.", nameof(orderId));
        if (productId == Guid.Empty) throw new ArgumentException("Product id is required.", nameof(productId));
        if (productVariantId == Guid.Empty) throw new ArgumentException("Variant id is required.", nameof(productVariantId));
        if (quantity <= 0m) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitPrice < 0m) throw new ArgumentOutOfRangeException(nameof(unitPrice));
        if (lineTotal < 0m) throw new ArgumentOutOfRangeException(nameof(lineTotal));

        return new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = productId,
            ProductVariantId = productVariantId,
            ProductName = RequireText(productName, NameMaxLength, nameof(productName)),
            VariantName = RequireText(variantName, NameMaxLength, nameof(variantName)),
            Sku = RequireText(sku, SkuMaxLength, nameof(sku)),
            SellingUnit = RequireText(sellingUnit, SellingUnitMaxLength, nameof(sellingUnit)),
            Quantity = quantity,
            UnitPrice = unitPrice,
            LineTotal = lineTotal
        };
    }

    private static string RequireText(string value, int max, string param)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.", param);
        var trimmed = value.Trim();
        if (trimmed.Length > max)
            throw new ArgumentException($"Value must be {max} characters or fewer.", param);
        return trimmed;
    }
}
