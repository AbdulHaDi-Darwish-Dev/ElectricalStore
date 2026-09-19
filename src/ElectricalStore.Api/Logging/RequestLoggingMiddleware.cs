using System.Diagnostics;

namespace ElectricalStore.Api.Logging;

/// <summary>
/// Completes one structured HTTP request log without reading bodies or sensitive headers.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private static readonly PathString HealthPath = new("/health");

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method)
            && context.Request.Path.StartsWithSegments(HealthPath, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var started = Stopwatch.GetTimestamp();
        try
        {
            await _next(context);
        }
        finally
        {
            var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            var route = ResolveRoute(context);
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            _logger.LogInformation(
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {ElapsedMs}ms TraceId={TraceId}",
                context.Request.Method,
                route,
                context.Response.StatusCode,
                (long)elapsedMs,
                traceId);
        }
    }

    private static string ResolveRoute(HttpContext context)
    {
        if (context.GetEndpoint() is RouteEndpoint routeEndpoint
            && !string.IsNullOrWhiteSpace(routeEndpoint.RoutePattern.RawText))
        {
            return routeEndpoint.RoutePattern.RawText;
        }

        return context.Request.Path.HasValue ? context.Request.Path.Value! : "/";
    }
}
