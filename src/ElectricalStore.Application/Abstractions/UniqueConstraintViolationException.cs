namespace ElectricalStore.Application.Abstractions;

/// <summary>
/// Raised by Infrastructure when SaveChanges hits a unique constraint/index violation.
/// Application maps this to expected conflict Results where appropriate.
/// </summary>
public sealed class UniqueConstraintViolationException : Exception
{
    public UniqueConstraintViolationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
