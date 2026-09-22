using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Customers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ElectricalStore.Application.Tests.Customers;

public sealed class CustomerEmailVerificationUseCaseTests
{
    [Fact]
    public async Task Resend_UnknownEmail_ReturnsGenericSuccessWithoutIssuing()
    {
        var gateway = new Mock<ICustomerEmailConfirmationGateway>();
        gateway.Setup(g => g.FindUserIdByEmailAsync("missing@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var useCase = new ResendCustomerEmailVerificationUseCase(
            gateway.Object,
            NullLogger<ResendCustomerEmailVerificationUseCase>.Instance);

        var result = await useCase.ExecuteAsync(
            new CustomerEmailVerificationResendRequest("missing@example.test"));

        Assert.True(result.IsSuccess);
        Assert.Equal(ResendCustomerEmailVerificationUseCase.PublicGenericMessage, result.Value.Message);
        gateway.Verify(
            g => g.TryRequestConfirmationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Resend_ConfirmedEmail_ReturnsGenericSuccessWithoutIssuing()
    {
        var userId = Guid.NewGuid();
        var gateway = new Mock<ICustomerEmailConfirmationGateway>();
        gateway.Setup(g => g.FindUserIdByEmailAsync("ok@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);
        gateway.Setup(g => g.IsEmailConfirmedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new ResendCustomerEmailVerificationUseCase(
            gateway.Object,
            NullLogger<ResendCustomerEmailVerificationUseCase>.Instance);

        var result = await useCase.ExecuteAsync(
            new CustomerEmailVerificationResendRequest("ok@example.test"));

        Assert.True(result.IsSuccess);
        Assert.Equal(ResendCustomerEmailVerificationUseCase.PublicGenericMessage, result.Value.Message);
        gateway.Verify(
            g => g.TryRequestConfirmationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Resend_UnconfirmedEmail_IssuesAndStillReturnsGenericSuccess()
    {
        var userId = Guid.NewGuid();
        var gateway = new Mock<ICustomerEmailConfirmationGateway>();
        gateway.Setup(g => g.FindUserIdByEmailAsync("new@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);
        gateway.Setup(g => g.IsEmailConfirmedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        gateway.Setup(g => g.TryRequestConfirmationAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerEmailConfirmationIssueOutcome(true, false, null));

        var useCase = new ResendCustomerEmailVerificationUseCase(
            gateway.Object,
            NullLogger<ResendCustomerEmailVerificationUseCase>.Instance);

        var result = await useCase.ExecuteAsync(
            new CustomerEmailVerificationResendRequest("new@example.test"));

        Assert.True(result.IsSuccess);
        Assert.Equal(ResendCustomerEmailVerificationUseCase.PublicGenericMessage, result.Value.Message);
        gateway.Verify(
            g => g.TryRequestConfirmationAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Resend_DeliveryFailure_StillReturnsGenericSuccess()
    {
        var userId = Guid.NewGuid();
        var gateway = new Mock<ICustomerEmailConfirmationGateway>();
        gateway.Setup(g => g.FindUserIdByEmailAsync("new@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);
        gateway.Setup(g => g.IsEmailConfirmedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        gateway.Setup(g => g.TryRequestConfirmationAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerEmailConfirmationIssueOutcome(
                false, false, "Verification.DeliveryFailed"));

        var useCase = new ResendCustomerEmailVerificationUseCase(
            gateway.Object,
            NullLogger<ResendCustomerEmailVerificationUseCase>.Instance);

        var result = await useCase.ExecuteAsync(
            new CustomerEmailVerificationResendRequest("new@example.test"));

        Assert.True(result.IsSuccess);
        Assert.Equal(ResendCustomerEmailVerificationUseCase.PublicGenericMessage, result.Value.Message);
    }

    [Fact]
    public async Task RegistrationConfirmation_DeliveryFailure_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var gateway = new Mock<ICustomerEmailConfirmationGateway>();
        gateway.Setup(g => g.TryRequestConfirmationAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerEmailConfirmationIssueOutcome(
                false, false, "Verification.DeliveryFailed"));

        var useCase = new RequestCustomerRegistrationEmailConfirmationUseCase(
            gateway.Object,
            NullLogger<RequestCustomerRegistrationEmailConfirmationUseCase>.Instance);

        Assert.False(await useCase.TrySendAsync(userId));
    }

    [Fact]
    public async Task RegistrationConfirmation_Sent_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var gateway = new Mock<ICustomerEmailConfirmationGateway>();
        gateway.Setup(g => g.TryRequestConfirmationAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerEmailConfirmationIssueOutcome(true, false, null));

        var useCase = new RequestCustomerRegistrationEmailConfirmationUseCase(
            gateway.Object,
            NullLogger<RequestCustomerRegistrationEmailConfirmationUseCase>.Instance);

        Assert.True(await useCase.TrySendAsync(userId));
    }
}
