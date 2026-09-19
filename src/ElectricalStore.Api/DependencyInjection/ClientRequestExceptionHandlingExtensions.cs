using ElectricalStore.Api.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ElectricalStore.Api.DependencyInjection;

/// <summary>
/// API-boundary exception handling for malformed client requests.
/// Must be registered before <c>AddPermixaProblemDetails</c> so this handler runs first.
/// </summary>
public static class ClientRequestExceptionHandlingExtensions
{
    public static IServiceCollection AddClientRequestExceptionHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<ClientRequestExceptionHandler>();
        return services;
    }
}
