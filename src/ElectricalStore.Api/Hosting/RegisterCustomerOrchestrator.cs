using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;
using ElectricalStore.Application.Customers;
using Permixa.Application.Authentication.Models;
using Permixa.Application.Authentication.Register;
using Permixa.AspNetCore.Http;
using AppResults = ElectricalStore.Api.Http.AppResultHttpExtensions;

namespace ElectricalStore.Api.Hosting;

/// <summary>
/// Customer registration across Permixa Identity + ElectricalStore CustomerProfile + email verification request.
/// Not a distributed transaction: identity first, then profile; compensate delete on profile failure.
/// Email delivery failure does not compensate/delete the account (Application policy via
/// <see cref="RequestCustomerRegistrationEmailConfirmationUseCase"/>).
/// </summary>
public sealed class RegisterCustomerOrchestrator
{
    private readonly RegisterUserUseCase _register;
    private readonly CompleteCustomerRegistrationUseCase _completeRegistration;
    private readonly RequestCustomerRegistrationEmailConfirmationUseCase _requestEmailConfirmation;
    private readonly ILogger<RegisterCustomerOrchestrator> _logger;

    public RegisterCustomerOrchestrator(
        RegisterUserUseCase register,
        CompleteCustomerRegistrationUseCase completeRegistration,
        RequestCustomerRegistrationEmailConfirmationUseCase requestEmailConfirmation,
        ILogger<RegisterCustomerOrchestrator> logger)
    {
        _register = register;
        _completeRegistration = completeRegistration;
        _requestEmailConfirmation = requestEmailConfirmation;
        _logger = logger;
    }

    public async Task<IResult> ExecuteAsync(
        CustomerRegistrationRequest request,
        HttpContext http,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fullNameCheck = UpdateCustomerProfileUseCase.ValidateFullName(request.FullName);
        if (fullNameCheck.IsFailure)
            return AppResults.ToHttpResult(fullNameCheck);

        if (string.IsNullOrWhiteSpace(request.Email))
            return AppResults.ToHttpResult(Result.Failure(CustomerErrors.EmailRequired));

        if (string.IsNullOrWhiteSpace(request.Password))
            return AppResults.ToHttpResult(Result.Failure(CustomerErrors.PasswordRequired));

        var email = request.Email.Trim();
        var userName = CustomerTechnicalUserName.Create();

        var registerResult = await _register.ExecuteAsync(
            new RegisterRequest
            {
                UserName = userName,
                Email = email,
                Password = request.Password
            },
            cancellationToken);

        if (!registerResult.IsSuccess || registerResult.Value is null)
            return registerResult.ToHttpResult(http, _ => Results.Empty);

        var registered = registerResult.Value;
        var completed = await _completeRegistration.ExecuteAsync(
            registered.UserId,
            registered.Email,
            request.FullName,
            cancellationToken);

        if (completed.IsFailure)
        {
            _logger.LogWarning(
                "Customer registration incomplete for {UserId}: {Code}",
                registered.UserId,
                completed.Error?.Code);
            return AppResults.ToHttpResult(completed);
        }

        var sent = await _requestEmailConfirmation.TrySendAsync(
            registered.UserId,
            cancellationToken);

        var body = completed.Value with
        {
            EmailVerificationRequired = true,
            VerificationEmailSent = sent
        };

        return Results.Json(body, statusCode: StatusCodes.Status201Created);
    }
}
