namespace ElectricalStore.Application.Abstractions;

/// <summary>Thrown when InventoryItem rowversion concurrency check fails.</summary>
public sealed class InventoryConcurrencyConflictException : Exception
{
    public InventoryConcurrencyConflictException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
