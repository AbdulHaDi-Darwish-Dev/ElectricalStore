using System.Diagnostics;

namespace ElectricalStore.Application.Logging;

/// <summary>Safe correlation helper for structured operational logs (no PII).</summary>
public static class LogCorrelation
{
    public static string TraceId =>
        Activity.Current?.Id
        ?? Activity.Current?.RootId
        ?? string.Empty;
}
