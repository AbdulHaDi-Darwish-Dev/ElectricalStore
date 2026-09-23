using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;
using Microsoft.Extensions.Logging;

namespace ElectricalStore.Application.Customers;

public sealed record CustomerEmailChangeRequestDto(string NewEmail, string CurrentPassword);

public sealed record CustomerEmailChangeRequestResult(string Message, string? PendingEmail);

public sealed record CustomerEmailChangeConfirmDto(Guid ChallengeId, string Token);

public sealed record CustomerEmailChangeConfirmResult(bool Changed, string? Email);

public sealed class RequestCustomerEmailChangeUseCase
{
    public const string SuccessMessage =
        "A confirmation email has been sent to the new address.";

    private readonly ICustomerEmailChangeGateway _gateway;
    private readonly ILogger<RequestCustomerEmailChangeUseCase> _logger;

    public RequestCustomerEmailChangeUseCase(
        ICustomerEmailChangeGateway gateway,
        ILogger<RequestCustomerEmailChangeUseCase> logger)
    {
        _gateway = gateway;
        _logger = logger;
    }

    public async Task<Result<CustomerEmailChangeRequestResult>> ExecuteAsync(
        Guid userId,
        CustomerEmailChangeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (userId == Guid.Empty)
            return Result.Failure<CustomerEmailChangeRequestResult>(CustomerErrors.ActorRequired);

        var newEmail = request.NewEmail?.Trim() ?? string.Empty;
        var currentPassword = request.CurrentPassword ?? string.Empty;

        if (string.IsNullOrWhiteSpace(newEmail))
            return Result.Failure<CustomerEmailChangeRequestResult>(CustomerErrors.EmailRequired);

        if (string.IsNullOrWhiteSpace(currentPassword))
            return Result.Failure<CustomerEmailChangeRequestResult>(CustomerErrors.PasswordRequired);

        if (await _gateway.IsDisabledAsync(userId, cancellationToken))
            return Result.Failure<CustomerEmailChangeRequestResult>(CustomerErrors.EmailChangeUnavailable);

        var outcome = await _gateway.RequestAsync(
            userId,
            newEmail,
            currentPassword,
            cancellationToken);

        if (outcome.Succeeded)
        {
            _logger.LogInformation("Email change requested for user {UserId}.", userId);
            return Result.Success(new CustomerEmailChangeRequestResult(
                SuccessMessage,
                outcome.PendingEmail));
        }

        return Result.Failure<CustomerEmailChangeRequestResult>(MapRequestError(outcome.ErrorCode));
    }

    private static Error MapRequestError(string? code) => code switch
    {
        "Authentication.CurrentPasswordInvalid" => CustomerErrors.CurrentPasswordInvalid,
        "Authentication.EmailAlreadyExists" => CustomerErrors.EmailAlreadyInUse,
        "Identity.DuplicateEmail" => CustomerErrors.EmailAlreadyInUse,
        "DuplicateEmail" => CustomerErrors.EmailAlreadyInUse,
        "Authentication.EmailUnchanged" => CustomerErrors.EmailUnchanged,
        "Verification.DeliveryFailed" => CustomerErrors.EmailChangeDeliveryFailed,
        "Verification.CooldownActive" => CustomerErrors.EmailChangeCooldownActive,
        _ => CustomerErrors.EmailChangeRequestFailed
    };
}

public sealed class ConfirmCustomerEmailChangeUseCase
{
    private readonly ICustomerEmailChangeGateway _gateway;
    private readonly ILogger<ConfirmCustomerEmailChangeUseCase> _logger;

    public ConfirmCustomerEmailChangeUseCase(
        ICustomerEmailChangeGateway gateway,
        ILogger<ConfirmCustomerEmailChangeUseCase> logger)
    {
        _gateway = gateway;
        _logger = logger;
    }

    public async Task<Result<CustomerEmailChangeConfirmResult>> ExecuteAsync(
        CustomerEmailChangeConfirmDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ChallengeId == Guid.Empty || string.IsNullOrWhiteSpace(request.Token))
            return Result.Failure<CustomerEmailChangeConfirmResult>(CustomerErrors.EmailChangeLinkInvalid);

        var outcome = await _gateway.ConfirmAsync(
            request.ChallengeId,
            request.Token.Trim(),
            cancellationToken);

        if (outcome.Succeeded)
        {
            _logger.LogInformation(
                "Email change confirmed for challenge {ChallengeId}.",
                request.ChallengeId);
            return Result.Success(new CustomerEmailChangeConfirmResult(true, outcome.NewEmail));
        }

        return Result.Failure<CustomerEmailChangeConfirmResult>(MapConfirmError(outcome.ErrorCode));
    }

    private static Error MapConfirmError(string? code) => code switch
    {
        "Verification.Expired" => CustomerErrors.EmailChangeLinkExpired,
        "Verification.AlreadyConsumed" => CustomerErrors.EmailChangeLinkUsed,
        "Verification.Invalidated" => CustomerErrors.EmailChangeLinkUsed,
        "Verification.InvalidToken" => CustomerErrors.EmailChangeLinkInvalid,
        "Verification.ChallengeNotFound" => CustomerErrors.EmailChangeLinkInvalid,
        "Verification.DestinationMismatch" => CustomerErrors.EmailChangeLinkInvalid,
        "Verification.UnsupportedMethod" => CustomerErrors.EmailChangeLinkInvalid,
        "Verification.UnsupportedPurpose" => CustomerErrors.EmailChangeLinkInvalid,
        "Authentication.EmailAlreadyExists" => CustomerErrors.EmailAlreadyInUse,
        "Identity.DuplicateEmail" => CustomerErrors.EmailAlreadyInUse,
        "DuplicateEmail" => CustomerErrors.EmailAlreadyInUse,
        "Customer.EmailChangeUnavailable" => CustomerErrors.EmailChangeUnavailable,
        "Customer.EmailChangeSessionRevocationFailed" =>
            CustomerErrors.EmailChangeSessionRevocationFailed,
        _ => CustomerErrors.EmailChangeConfirmFailed
    };
}
