using Permixa.Application.Authentication.Login;
using Permixa.Application.Authentication.Logout;
using Permixa.Application.Authentication.Models;
using Permixa.Application.Authentication.Refresh;
using Permixa.Application.Authentication.Register;
using Permixa.AspNetCore.Http;
using Permixa.AspNetCore.RateLimiting;

namespace ElectricalStore.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/auth").WithTags("Authentication");

        auth.MapPost("/register", async (
            RegisterRequest request,
            RegisterUserUseCase useCase,
            HttpContext http) =>
        {
            var result = await useCase.ExecuteAsync(request);
            return result.ToHttpResult(http, value =>
                Results.Json(value, statusCode: StatusCodes.Status201Created));
        })
            .WithName("Register")
            .WithSummary("Register a new user");

        auth.MapPost("/login", async (
            LoginRequest request,
            LoginUseCase useCase,
            HttpContext http) =>
        {
            var result = await useCase.ExecuteAsync(request);
            return result.ToHttpResult(http, value =>
                value.Authentication is not null
                    ? Results.Json(value.Authentication)
                    : Results.Json(value.Mfa));
        })
            .RequireRateLimiting("Login")
            .WithName("Login")
            .WithSummary("Login");

        auth.MapPost("/refresh", async (
            RefreshTokenRequest request,
            RefreshAccessTokenUseCase useCase,
            HttpContext http) =>
        {
            var result = await useCase.ExecuteAsync(request);
            return result.ToHttpResult(http, value => Results.Json(value));
        })
            .WithName("Refresh")
            .WithSummary("Refresh access token");

        auth.MapPost("/logout", async (
            RevokeRefreshTokenRequest request,
            LogoutUseCase useCase,
            HttpContext http) =>
        {
            var result = await useCase.ExecuteAsync(request);
            return result.ToHttpResult(http, () => Results.Ok());
        })
            .WithName("Logout")
            .WithSummary("Logout / revoke refresh token");

        return app;
    }
}
