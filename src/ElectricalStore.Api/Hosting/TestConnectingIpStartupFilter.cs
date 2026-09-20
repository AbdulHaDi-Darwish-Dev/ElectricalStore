using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElectricalStore.Api.Hosting;

/// <summary>
/// Test-only: sets <see cref="ConnectionInfo.RemoteIpAddress"/> from
/// <c>X-Test-Connecting-Ip</c> before ForwardedHeaders runs, so TestServer can simulate
/// a trusted vs untrusted TCP peer. Gated by Testing:AllowConnectingIpOverride.
/// </summary>
public sealed class TestConnectingIpStartupFilter : IStartupFilter
{
    public const string HeaderName = "X-Test-Connecting-Ip";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
            if (configuration.GetValue("Testing:AllowConnectingIpOverride", false))
            {
                app.Use(async (context, nextMiddleware) =>
                {
                    if (context.Request.Headers.TryGetValue(HeaderName, out var values)
                        && IPAddress.TryParse(values.ToString(), out var ip))
                    {
                        context.Connection.RemoteIpAddress = ip;
                    }

                    await nextMiddleware();
                });
            }

            next(app);
        };
    }
}
