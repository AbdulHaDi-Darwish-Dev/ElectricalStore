namespace ElectricalStore.Application.Common;

/// <summary>Stable, machine-readable application error for expected business failures.</summary>
public sealed class Error
{
    public Error(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Code = code;
        Message = message;
    }

    public string Code { get; }

    public string Message { get; }
}
