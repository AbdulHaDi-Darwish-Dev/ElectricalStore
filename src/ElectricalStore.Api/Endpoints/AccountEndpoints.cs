using ElectricalStore.Api.Hosting;
using ElectricalStore.Api.Http;
using ElectricalStore.Application.Customers;
using Permixa.Application.Authentication.ChangePassword;
using Permixa.Application.Verification.EmailConfirmation;
using Permixa.AspNetCore.Http;
using Permixa.AspNetCore.RateLimiting;
using Permixa.AspNetCore.Security;

namespace ElectricalStore.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var account = app.MapGroup("/account")
            .WithTags("Account");

        account.MapPost("/register", async (
                CustomerRegistrationRequest request,
                RegisterCustomerOrchestrator orchestrator,
                HttpContext http,
                CancellationToken cancellationToken) =>
            await orchestrator.ExecuteAsync(request, http, cancellationToken))
            .AllowAnonymous()
            .WithName("RegisterCustomer")
            .WithSummary("Register a storefront customer (FullName + email + password; technical UserName is server-owned)");

        account.MapPost("/email-verification/confirm", async (
                ConfirmCustomerEmailRequest request,
                ConfirmEmailUseCase useCase,
                HttpContext http,
                CancellationToken cancellationToken) =>
            {
                ArgumentNullException.ThrowIfNull(request);

                var result = await useCase.ExecuteAsync(
                    new ConfirmEmailRequest
                    {
                        ChallengeId = request.ChallengeId,
                        VerificationValue = request.Token
                    },
                    cancellationToken);

                // Idempotent UX: already confirmed / already consumed challenge → success.
                if (!result.IsSuccess)
                {
                    var code = result.Error?.Code;
                    if (code is "Verification.AlreadyConfirmed" or "Verification.AlreadyConsumed")
                    {
                        return Results.Ok(new { confirmed = true, alreadyConfirmed = true });
                    }
                }

                return result.ToHttpResult(http, () => Results.Ok(new
                {
                    confirmed = true,
                    alreadyConfirmed = false
                }));
            })
            .AllowAnonymous()
            .WithName("ConfirmCustomerEmail")
            .WithSummary("Confirm customer email using Permixa verification challenge + token");

        account.MapPost("/email-verification/resend", async (
                CustomerEmailVerificationResendRequest request,
                ResendCustomerEmailVerificationUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(request, cancellationToken);
                return result.ToHttpResult();
            })
            .AllowAnonymous()
            .RequireRateLimiting("EmailVerificationResend")
            .WithName("ResendCustomerEmailVerification")
            .WithSummary("Resend registration email verification (anti-enumeration public response)");

        account.MapGet("/profile", async (
                ICurrentUser currentUser,
                GetCustomerProfileUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is null)
                    return Results.Unauthorized();

                var result = await useCase.ExecuteAsync(currentUser.UserId.Value, cancellationToken);
                return result.ToHttpResult();
            })
            .RequireAuthorization()
            .WithName("GetCustomerProfile")
            .WithSummary("Get the authenticated customer's profile");

        account.MapPut("/profile", async (
                UpdateCustomerProfileRequest request,
                ICurrentUser currentUser,
                UpdateCustomerProfileUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is null)
                    return Results.Unauthorized();

                var result = await useCase.ExecuteAsync(
                    currentUser.UserId.Value,
                    request,
                    cancellationToken);
                return result.ToHttpResult();
            })
            .RequireAuthorization()
            .WithName("UpdateCustomerProfile")
            .WithSummary("Update the authenticated customer's FullName only");

        account.MapPost("/change-password", async (
                ChangeCustomerPasswordRequest request,
                ICurrentUser currentUser,
                ChangePasswordUseCase useCase,
                HttpContext http,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is null)
                    return Results.Unauthorized();

                ArgumentNullException.ThrowIfNull(request);

                var result = await useCase.ExecuteAsync(
                    new ChangePasswordRequest
                    {
                        UserId = currentUser.UserId.Value,
                        CurrentPassword = request.CurrentPassword,
                        NewPassword = request.NewPassword
                    },
                    cancellationToken);

                return result.ToHttpResult(http, value => Results.Ok(new
                {
                    reauthenticationRequired = value.ReauthenticationRequired
                }));
            })
            .RequireAuthorization()
            .WithName("ChangeCustomerPassword")
            .WithSummary("Change password for the authenticated customer (Permixa ChangePasswordUseCase)");

        return app;
    }
}
