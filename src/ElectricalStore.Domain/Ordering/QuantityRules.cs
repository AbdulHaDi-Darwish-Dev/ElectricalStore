namespace ElectricalStore.Domain.Ordering;

/// <summary>Decimal-safe quantity increment checks (no floating-point modulo).</summary>
public static class QuantityRules
{
    public const int QuantityPrecision = 18;
    public const int QuantityScale = 3;

    public static bool IsValidQuantity(decimal quantity, decimal quantityIncrement)
    {
        if (quantity <= 0m || quantityIncrement <= 0m)
            return false;

        var quotient = quantity / quantityIncrement;
        return quotient == decimal.Truncate(quotient);
    }
}
