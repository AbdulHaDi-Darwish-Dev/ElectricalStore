using ElectricalStore.Api.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace ElectricalStore.Api.DependencyInjection;

/// <summary>Pure Api-host registrations (OpenAPI / Swagger). Not business or infrastructure.</summary>
public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "ElectricalStore API",
                Version = "v1"
            });

            // Documentation/testing aid for the existing JWT Bearer scheme (AddPermixaJwtBearer).
            // Does not register or change authentication handlers.
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Existing Permixa JWT access token. Paste the token only (Swagger sends Authorization: Bearer {token}).",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.OperationFilter<BearerSecurityOperationFilter>();
            options.OperationFilter<GuestOrderTokenOperationFilter>();
            options.OperationFilter<IdempotencyKeyOperationFilter>();
        });

        services.AddOptions<Microsoft.AspNetCore.Http.Features.FormOptions>()
            .Configure<ElectricalStore.Application.Media.MediaOptions>((form, media) =>
            {
                // Allow slightly oversize multipart so ImageUploadValidator can return Media.FileTooLarge.
                form.MultipartBodyLengthLimit = media.MaxImageSizeBytes + (2L * 1024 * 1024);
            });

        return services;
    }
}
