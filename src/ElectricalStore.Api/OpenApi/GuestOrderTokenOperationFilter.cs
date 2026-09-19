using ElectricalStore.Api.Endpoints;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ElectricalStore.Api.OpenApi;

/// <summary>Documents X-Order-Token for guest order endpoints. Documentation only.</summary>
public sealed class GuestOrderTokenOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var name = context.ApiDescription.ActionDescriptor.EndpointMetadata
                       .OfType<EndpointNameMetadata>()
                       .FirstOrDefault()?.EndpointName
                   ?? operation.OperationId
                   ?? string.Empty;

        var path = context.ApiDescription.RelativePath ?? string.Empty;
        var isAdmin = path.StartsWith("admin/", StringComparison.OrdinalIgnoreCase);

        var isGuestTrack = string.Equals(name, "TrackGuestOrder", StringComparison.Ordinal)
                           || path.Contains("/track", StringComparison.OrdinalIgnoreCase);
        var isGuestMutate = string.Equals(name, "CancelMyOrder", StringComparison.Ordinal)
                            || string.Equals(name, "ModifyMyPendingOrder", StringComparison.Ordinal);

        if (!isGuestTrack && !isGuestMutate)
            return;

        if (isAdmin)
            return;

        // Only order track paths (avoid accidental matches).
        if (isGuestTrack && !path.Contains("orders/", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(name, "TrackGuestOrder", StringComparison.Ordinal))
            return;

        operation.Parameters ??= new List<OpenApiParameter>();
        if (operation.Parameters.Any(p => p.Name == OrderingEndpoints.GuestOrderTokenHeader))
            return;

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = OrderingEndpoints.GuestOrderTokenHeader,
            In = ParameterLocation.Header,
            Required = isGuestTrack || string.Equals(name, "TrackGuestOrder", StringComparison.Ordinal),
            Description =
                "Guest order access token returned once from Place Order (guestAccessToken). " +
                "Required for guest track; used for guest cancel/modify when not authenticated. " +
                "Not a JWT substitute."
        });
    }
}
