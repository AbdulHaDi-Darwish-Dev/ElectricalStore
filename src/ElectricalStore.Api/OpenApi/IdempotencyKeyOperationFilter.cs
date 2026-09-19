using ElectricalStore.Api.Endpoints;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ElectricalStore.Api.OpenApi;

/// <summary>Documents Idempotency-Key for Place Order. Documentation only.</summary>
public sealed class IdempotencyKeyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var name = context.ApiDescription.ActionDescriptor.EndpointMetadata
                       .OfType<EndpointNameMetadata>()
                       .FirstOrDefault()?.EndpointName
                   ?? operation.OperationId
                   ?? string.Empty;

        if (!string.Equals(name, "PlaceOrder", StringComparison.Ordinal))
            return;

        operation.Parameters ??= new List<OpenApiParameter>();
        if (operation.Parameters.Any(p => p.Name == OrderingEndpoints.IdempotencyKeyHeader))
            return;

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = OrderingEndpoints.IdempotencyKeyHeader,
            In = ParameterLocation.Header,
            Required = true,
            Description =
                "Client-generated opaque high-entropy key (16–128 chars). " +
                "Retries with the same key + same auth/guest scope reuse the created Order " +
                "(including guestAccessToken for guest orders). Not a substitute for guest X-Order-Token."
        });
    }
}
