using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;
using Microsoft.Extensions.Logging;

namespace ElectricalStore.Application.Customers;

/// <summary>
/// Public resend of registration email confirmation.
/// Anti-enumeration: callers always receive the same success shape whether or not mail was sent.
/// </summary>
public sealed class ResendCustomerEmailVerificationUseCase
{
    public const string PublicGenericMessage =
        "If an eligible account exists, a verification email has been sent.";

    private readonly ICustomerEmailConfirmationGateway _gateway;
    private readonly ILogger<ResendCustomerEmailVerificationUseCase> _logger;

    public ResendCustomerEmailVerificationUseCase(
        ICustomerEmailConfirmationGateway gateway,
        ILogger<ResendCustomerEmailVerificationUseCase> logger)
    {
        _gateway = gateway;
        _logger = logger;
    }

    public async Task<Result<CustomerEmailVerificationResendResult>> ExecuteAsync(
        CustomerEmailVerificationResendRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = request.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<CustomerEmailVerificationResendResult>(CustomerErrors.EmailRequired);

        var generic = new CustomerEmailVerificationResendResult(PublicGenericMessage);

        Guid? userId;
        try
        {
            userId = await _gateway.FindUserIdByEmailAsync(email, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email verification resend lookup failed.");
            return Result.Success(generic);
        }

        if (userId is null)
            return Result.Success(generic);

        bool confirmed;
        try
        {
            confirmed = await _gateway.IsEmailConfirmedAsync(userId.Value, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email verification resend confirmation check failed for {UserId}.", userId);
            return Result.Success(generic);
        }

        if (confirmed)
            return Result.Success(generic);

        var issue = await _gateway.TryRequestConfirmationAsync(userId.Value, cancellationToken);
        if (!issue.Sent)
        {
            _logger.LogWarning(
                "Email verification resend not delivered for {UserId}: {Code}",
                userId.Value,
                issue.ErrorCode ?? (issue.AlreadyConfirmed ? "AlreadyConfirmed" : "NotSent"));
        }

        return Result.Success(generic);
    }
}

/// <summary>
/// Issues confirmation after successful customer registration.
/// Delivery failure does not roll back identity/profile creation — returns false so the host can report it.
/// </summary>
public sealed class RequestCustomerRegistrationEmailConfirmationUseCase
{
    private readonly ICustomerEmailConfirmationGateway _gateway;
    private readonly ILogger<RequestCustomerRegistrationEmailConfirmationUseCase> _logger;

    public RequestCustomerRegistrationEmailConfirmationUseCase(
        ICustomerEmailConfirmationGateway gateway,
        ILogger<RequestCustomerRegistrationEmailConfirmationUseCase> logger)
    {
        _gateway = gateway;
        _logger = logger;
    }

    public async Task<bool> TrySendAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var outcome = await _gateway.TryRequestConfirmationAsync(userId, cancellationToken);
        if (outcome.Sent)
            return true;

        if (outcome.AlreadyConfirmed)
            return false;

        _logger.LogWarning(
            "Registration verification email not sent for {UserId}: {Code}",
            userId,
            outcome.ErrorCode ?? "NotSent");
        return false;
    }
}
