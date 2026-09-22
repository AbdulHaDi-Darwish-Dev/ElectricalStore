using ElectricalStore.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Permixa.Application.Verification.Abstractions;
using Permixa.Application.Verification.EmailConfirmation;
using Permixa.Domain.Verification;

namespace ElectricalStore.Api.Hosting;

/// <summary>
/// Thin Permixa adapter for Application email-confirmation policy use cases.
/// </summary>
public sealed class PermixaCustomerEmailConfirmationGateway : ICustomerEmailConfirmationGateway
{
    private readonly IIdentityUserEmailReader _emails;
    private readonly RequestEmailConfirmationUseCase _requestConfirmation;
    private readonly ILogger<PermixaCustomerEmailConfirmationGateway> _logger;

    public PermixaCustomerEmailConfirmationGateway(
        IIdentityUserEmailReader emails,
        RequestEmailConfirmationUseCase requestConfirmation,
        ILogger<PermixaCustomerEmailConfirmationGateway> logger)
    {
        _emails = emails;
        _requestConfirmation = requestConfirmation;
        _logger = logger;
    }

    public Task<Guid?> FindUserIdByEmailAsync(
        string email,
        CancellationToken cancellationToken = default) =>
        _emails.FindUserIdByEmailAsync(email, cancellationToken);

    public Task<bool> IsEmailConfirmedAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _emails.IsEmailConfirmedAsync(userId, cancellationToken);

    public async Task<CustomerEmailConfirmationIssueOutcome> TryRequestConfirmationAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _requestConfirmation.ExecuteAsync(
                new RequestEmailConfirmationRequest
                {
                    UserId = userId,
                    Method = VerificationMethod.UrlToken
                },
                cancellationToken);

            if (result.IsSuccess && result.Value is not null)
            {
                var already = result.Value.AlreadyConfirmed;
                return new CustomerEmailConfirmationIssueOutcome(
                    Sent: !already,
                    AlreadyConfirmed: already,
                    ErrorCode: null);
            }

            return new CustomerEmailConfirmationIssueOutcome(
                Sent: false,
                AlreadyConfirmed: false,
                ErrorCode: result.Error?.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email confirmation request failed for {UserId}.", userId);
            return new CustomerEmailConfirmationIssueOutcome(
                Sent: false,
                AlreadyConfirmed: false,
                ErrorCode: "Verification.DeliveryFailed");
        }
    }
}
