using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;
using Microsoft.Extensions.Logging;

namespace ElectricalStore.Application.Customers;

public sealed record CustomerPasswordForgotRequest(string Email);

public sealed record CustomerPasswordForgotResult(string Message);

public sealed record CustomerPasswordResetRequest(
    Guid ChallengeId,
    string Token,
    string NewPassword);

/// <summary>
/// Public forgot-password request. Anti-enumeration: same success shape whether or not mail was sent.
/// Disabled accounts are treated as ineligible (no send). Unconfirmed accounts may receive reset mail
/// but remain login-blocked under RequireConfirmedEmail.
/// </summary>
public sealed class RequestCustomerPasswordResetUseCase
{
    public const string PublicGenericMessage =
        "If an eligible account exists, a password reset email has been sent.";

    private readonly ICustomerPasswordResetGateway _gateway;
    private readonly ILogger<RequestCustomerPasswordResetUseCase> _logger;

    public RequestCustomerPasswordResetUseCase(
        ICustomerPasswordResetGateway gateway,
        ILogger<RequestCustomerPasswordResetUseCase> logger)
    {
        _gateway = gateway;
        _logger = logger;
    }

    public async Task<Result<CustomerPasswordForgotResult>> ExecuteAsync(
        CustomerPasswordForgotRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = request.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<CustomerPasswordForgotResult>(CustomerErrors.EmailRequired);

        var generic = new CustomerPasswordForgotResult(PublicGenericMessage);

        Guid? userId;
        try
        {
            userId = await _gateway.FindUserIdByEmailAsync(email, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password reset request lookup failed.");
            return Result.Success(generic);
        }

        if (userId is null)
            return Result.Success(generic);

        try
        {
            if (await _gateway.IsDisabledAsync(userId.Value, cancellationToken))
                return Result.Success(generic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password reset disabled-check failed for {UserId}.", userId);
            return Result.Success(generic);
        }

        try
        {
            await _gateway.TryRequestResetEmailAsync(email, cancellationToken);
        }
        catch (Exception ex)
        {
            // Provider/outage: do not reveal whether an account exists.
            _logger.LogError(ex, "Password reset email delivery failed for {UserId}.", userId);
        }

        return Result.Success(generic);
    }
}

/// <summary>
/// Completes public password reset using Permixa challenge + Identity token.
/// </summary>
public sealed class ResetCustomerPasswordUseCase
{
    private readonly ICustomerPasswordResetGateway _gateway;

    public ResetCustomerPasswordUseCase(ICustomerPasswordResetGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<Result> ExecuteAsync(
        CustomerPasswordResetRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ChallengeId == Guid.Empty || string.IsNullOrWhiteSpace(request.Token))
            return Result.Failure(CustomerErrors.PasswordResetLinkInvalid);

        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return Result.Failure(CustomerErrors.PasswordRequired);

        var outcome = await _gateway.ResetPasswordAsync(
            request.ChallengeId,
            request.Token.Trim(),
            request.NewPassword,
            cancellationToken);

        if (outcome.Succeeded)
            return Result.Success();

        return Result.Failure(MapError(outcome.ErrorCode));
    }

    private static Error MapError(string? code) => code switch
    {
        "Verification.Expired" => CustomerErrors.PasswordResetLinkExpired,
        "Verification.AlreadyConsumed" => CustomerErrors.PasswordResetLinkUsed,
        "Verification.Invalidated" => CustomerErrors.PasswordResetLinkUsed,
        "Verification.InvalidToken" => CustomerErrors.PasswordResetLinkInvalid,
        "Verification.ChallengeNotFound" => CustomerErrors.PasswordResetLinkInvalid,
        "Verification.DestinationMismatch" => CustomerErrors.PasswordResetLinkInvalid,
        "Verification.UnsupportedMethod" => CustomerErrors.PasswordResetLinkInvalid,
        "Authentication.InvalidPassword" => CustomerErrors.PasswordPolicyFailed,
        "Customer.PasswordResetSessionRevocationFailed" =>
            CustomerErrors.PasswordResetSessionRevocationFailed,
        _ => CustomerErrors.PasswordResetFailed
    };
}
