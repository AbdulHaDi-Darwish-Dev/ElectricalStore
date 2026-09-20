using ElectricalStore.Api.DependencyInjection;
using ElectricalStore.Api.Endpoints;
using ElectricalStore.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Missing ConnectionStrings:Default. Run scripts/init-dev-secrets and/or set User Secrets / environment.");

builder.Services
    // Must register before AddPermixaProblemDetails so BadHttpRequestException/JsonException
    // map to 400 instead of Permixa's catch-all 500 InternalError.
    .AddClientRequestExceptionHandling()
    .AddAppInfrastructure(connectionString, builder.Configuration)
    .AddPermixaHost(builder.Configuration, builder.Environment, connectionString)
    .AddApiServices()
    .AddFrontendCors(builder.Configuration);

var app = builder.Build();

await app.InitializeDevelopmentAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
// After ExceptionHandler so 401/403/ProblemDetails responses still receive CORS headers
// for allowed origins; before Authentication so preflight OPTIONS is not challenged.
app.UseCors(FrontendCorsServiceCollectionExtensions.FrontendPolicyName);
app.UseElectricalStoreRequestLogging();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapApiEndpoints();

app.Run();

public partial class Program;
