using ElectricalStore.Api.Logging;

namespace ElectricalStore.Api.DependencyInjection;

public static class LoggingHostExtensions
{
    public static IApplicationBuilder UseElectricalStoreRequestLogging(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestLoggingMiddleware>();
}
