using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Customers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ElectricalStore.Application.Tests.Customers;

public sealed class CustomerEmailChangeUseCaseTests
{
    [Fact]
    public async Task Request_Valid_ReturnsSuccessWithPendingEmail()
    {
        var userId = Guid.NewGuid();
        var gateway = new Mock<ICustomerEmailChangeGateway>();
        gateway.Setup(g => g.IsDisabledAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        gateway.Setup(g => g.RequestAsync(
                userId,
                "new@example.test",
                "Current-Pass-1!",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerEmailChangeRequestOutcome(true, null, "new@example.test"));

        var useCase = new RequestCustomerEmailChangeUseCase(
            gateway.Object,
            NullLogger<RequestCustomerEmailChangeUseCase>.Instance);

        var result = await useCase.ExecuteAsync(
            userId,
            new CustomerEmailChangeRequestDto("new@example.test", "Current-Pass-1!"));

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestCustomerEmailChangeUseCase.SuccessMessage, result.Value.Message);
        Assert.Equal("new@example.test", result.Value.PendingEmail);
    }

    [Fact]
    public async Task Request_WrongCurrentPassword_MapsError()
    {
        var userId = Guid.NewGuid();
        var gateway = new Mock<ICustomerEmailChangeGateway>();
        gateway.Setup(g => g.IsDisabledAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        gateway.Setup(g => g.RequestAsync(
                userId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerEmailChangeRequestOutcome(
                false,
                "Authentication.CurrentPasswordInvalid"));

        var useCase = new RequestCustomerEmailChangeUseCase(
            gateway.Object,
            NullLogger<RequestCustomerEmailChangeUseCase>.Instance);

        var result = await useCase.ExecuteAsync(
            userId,
            new CustomerEmailChangeRequestDto("new@example.test", "wrong"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrors.CurrentPasswordInvalid.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Request_SameEmail_MapsUnchanged()
    {
        var userId = Guid.NewGuid();
        var gateway = new Mock<ICustomerEmailChangeGateway>();
        gateway.Setup(g => g.IsDisabledAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        gateway.Setup(g => g.RequestAsync(
                userId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerEmailChangeRequestOutcome(
                false,
                "Authentication.EmailUnchanged"));

        var useCase = new RequestCustomerEmailChangeUseCase(
            gateway.Object,
            NullLogger<RequestCustomerEmailChangeUseCase>.Instance);

        var result = await useCase.ExecuteAsync(
            userId,
            new CustomerEmailChangeRequestDto("same@example.test", "Current-Pass-1!"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrors.EmailUnchanged.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Request_DuplicateEmail_MapsConflict()
    {
        var userId = Guid.NewGuid();
        var gateway = new Mock<ICustomerEmailChangeGateway>();
        gateway.Setup(g => g.IsDisabledAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        gateway.Setup(g => g.RequestAsync(
                userId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerEmailChangeRequestOutcome(
                false,
                "Authentication.EmailAlreadyExists"));

        var useCase = new RequestCustomerEmailChangeUseCase(
            gateway.Object,
            NullLogger<RequestCustomerEmailChangeUseCase>.Instance);

        var result = await useCase.ExecuteAsync(
            userId,
            new CustomerEmailChangeRequestDto("taken@example.test", "Current-Pass-1!"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrors.EmailAlreadyInUse.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Request_DeliveryFailure_Maps503Code()
    {
        var userId = Guid.NewGuid();
        var gateway = new Mock<ICustomerEmailChangeGateway>();
        gateway.Setup(g => g.IsDisabledAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        gateway.Setup(g => g.RequestAsync(
                userId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerEmailChangeRequestOutcome(
                false,
                "Verification.DeliveryFailed"));

        var useCase = new RequestCustomerEmailChangeUseCase(
            gateway.Object,
            NullLogger<RequestCustomerEmailChangeUseCase>.Instance);

        var result = await useCase.ExecuteAsync(
            userId,
            new CustomerEmailChangeRequestDto("new@example.test", "Current-Pass-1!"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrors.EmailChangeDeliveryFailed.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Request_DisabledAccount_FailsWithoutGatewayCall()
    {
        var userId = Guid.NewGuid();
        var gateway = new Mock<ICustomerEmailChangeGateway>();
        gateway.Setup(g => g.IsDisabledAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new RequestCustomerEmailChangeUseCase(
            gateway.Object,
            NullLogger<RequestCustomerEmailChangeUseCase>.Instance);

        var result = await useCase.ExecuteAsync(
            userId,
            new CustomerEmailChangeRequestDto("new@example.test", "Current-Pass-1!"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrors.EmailChangeUnavailable.Code, result.Error!.Code);
        gateway.Verify(
            g => g.RequestAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Request_EmptyUserId_Fails()
    {
        var gateway = new Mock<ICustomerEmailChangeGateway>();
        var useCase = new RequestCustomerEmailChangeUseCase(
            gateway.Object,
            NullLogger<RequestCustomerEmailChangeUseCase>.Instance);

        var result = await useCase.ExecuteAsync(
            Guid.Empty,
            new CustomerEmailChangeRequestDto("new@example.test", "Current-Pass-1!"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrors.ActorRequired.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Confirm_Success_ReturnsChanged()
    {
        var challengeId = Guid.NewGuid();
        var gateway = new Mock<ICustomerEmailChangeGateway>();
        gateway.Setup(g => g.ConfirmAsync(
                challengeId,
                "tok",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerEmailChangeConfirmOutcome(true, null, "new@example.test"));

        var useCase = new ConfirmCustomerEmailChangeUseCase(
            gateway.Object,
            NullLogger<ConfirmCustomerEmailChangeUseCase>.Instance);

        var result = await useCase.ExecuteAsync(
            new CustomerEmailChangeConfirmDto(challengeId, "tok"));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Changed);
        Assert.Equal("new@example.test", result.Value.Email);
    }

    [Fact]
    public async Task Confirm_InvalidLink_MapsError()
    {
        var challengeId = Guid.NewGuid();
        var gateway = new Mock<ICustomerEmailChangeGateway>();
        gateway.Setup(g => g.ConfirmAsync(
                challengeId,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerEmailChangeConfirmOutcome(
                false,
                "Verification.InvalidToken"));

        var useCase = new ConfirmCustomerEmailChangeUseCase(
            gateway.Object,
            NullLogger<ConfirmCustomerEmailChangeUseCase>.Instance);

        var result = await useCase.ExecuteAsync(
            new CustomerEmailChangeConfirmDto(challengeId, "bad"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrors.EmailChangeLinkInvalid.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Confirm_WhenSessionRevocationFails_DoesNotReportSuccess()
    {
        var challengeId = Guid.NewGuid();
        var gateway = new Mock<ICustomerEmailChangeGateway>();
        gateway.Setup(g => g.ConfirmAsync(
                challengeId,
                "tok",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerEmailChangeConfirmOutcome(
                false,
                "Customer.EmailChangeSessionRevocationFailed",
                "new@example.test",
                "old@example.test"));

        var useCase = new ConfirmCustomerEmailChangeUseCase(
            gateway.Object,
            NullLogger<ConfirmCustomerEmailChangeUseCase>.Instance);

        var result = await useCase.ExecuteAsync(
            new CustomerEmailChangeConfirmDto(challengeId, "tok"));

        Assert.False(result.IsSuccess);
        Assert.Equal(
            CustomerErrors.EmailChangeSessionRevocationFailed.Code,
            result.Error!.Code);
    }

    [Fact]
    public async Task Confirm_EmptyChallenge_FailsValidation()
    {
        var gateway = new Mock<ICustomerEmailChangeGateway>();
        var useCase = new ConfirmCustomerEmailChangeUseCase(
            gateway.Object,
            NullLogger<ConfirmCustomerEmailChangeUseCase>.Instance);

        var result = await useCase.ExecuteAsync(
            new CustomerEmailChangeConfirmDto(Guid.Empty, "tok"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrors.EmailChangeLinkInvalid.Code, result.Error!.Code);
        gateway.Verify(
            g => g.ConfirmAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
