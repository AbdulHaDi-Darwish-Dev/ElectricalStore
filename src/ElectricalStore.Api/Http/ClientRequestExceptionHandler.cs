using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ElectricalStore.Api.Http;

/// <summary>
/// Maps client request binding/JSON failures to HTTP 400 ProblemDetails at the API boundary.
/// Runs before <c>PermixaExceptionHandler</c> (registration order). Does not handle unexpected server faults.
/// </summary>
public sealed class ClientRequestExceptionHandler : IExceptionHandler
{
    public const string ErrorCode = "InvalidRequest";

    private const string SafeDetail = "The request body is invalid or could not be parsed.";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (!TryMapClientError(exception, out var statusCode))
            return false;

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = "Bad Request",
            Detail = SafeDetail,
            Extensions =
            {
                ["code"] = ErrorCode,
                ["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier
            }
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken);

        return true;
    }

    private static bool TryMapClientError(Exception exception, out int statusCode)
    {
        switch (exception)
        {
            case BadHttpRequestException bad:
                // Minimal API JSON/body binding failures throw BadHttpRequestException with a 4xx StatusCode.
                statusCode = bad.StatusCode is >= StatusCodes.Status400BadRequest
                    and < StatusCodes.Status500InternalServerError
                    ? bad.StatusCode
                    : StatusCodes.Status400BadRequest;
                return true;

            case JsonException:
                statusCode = StatusCodes.Status400BadRequest;
                return true;

            default:
                statusCode = StatusCodes.Status400BadRequest;
                return false;
        }
    }
}
