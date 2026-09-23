using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Customers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ElectricalStore.Application.Tests.Customers;

public sealed class CustomerPasswordResetUseCaseTests
{
    [Fact]
    public async Task Forgot_UnknownEmail_ReturnsGenericSuccessWithoutSending()
    {
        var gateway = new Mock<ICustomerPasswordResetGateway>();
        gateway.Setup(g => g.FindUserIdByEmailAsync("missing@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var useCase = new RequestCustomerPasswordResetUseCase(
            gateway.Object,
            NullLogger<RequestCustomerPasswordResetUseCase>.Instance);

        var result = await useCase.ExecuteAsync(
            new CustomerPasswordForgotRequest("missing@example.test"));

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestCustomerPasswordResetUseCase.PublicGenericMessage, result.Value.Message);
        gateway.Verify(
            g => g.TryRequestResetEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Forgot_EligibleEmail_SendsAndReturnsGenericSuccess()
    {
        var userId = Guid.NewGuid();
        var gateway = new Mock<ICustomerPasswordResetGateway>();
        gateway.Setup(g => g.FindUserIdByEmailAsync("ok@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);
        gateway.Setup(g => g.IsDisabledAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        gateway.Setup(g => g.TryRequestResetEmailAsync("ok@example.test", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new RequestCustomerPasswordResetUseCase(
            gateway.Object,
            NullLogger<RequestCustomerPasswordResetUseCase>.Instance);

        var result = await useCase.ExecuteAsync(
            new CustomerPasswordForgotRequest("ok@example.test"));

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestCustomerPasswordResetUseCase.PublicGenericMessage, result.Value.Message);
        gateway.Verify(
            g => g.TryRequestResetEmailAsync("ok@example.test", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Forgot_DisabledAccount_ReturnsGenericSuccessWithoutSending()
    {
        var userId = Guid.NewGuid();
        var gateway = new Mock<ICustomerPasswordResetGateway>();
        gateway.Setup(g => g.FindUserIdByEmailAsync("disabled@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);
        gateway.Setup(g => g.IsDisabledAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new RequestCustomerPasswordResetUseCase(
            gateway.Object,
            NullLogger<RequestCustomerPasswordResetUseCase>.Instance);

        var result = await useCase.ExecuteAsync(
            new CustomerPasswordForgotRequest("disabled@example.test"));

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestCustomerPasswordResetUseCase.PublicGenericMessage, result.Value.Message);
        gateway.Verify(
            g => g.TryRequestResetEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Forgot_ProviderThrows_StillReturnsGenericSuccess()
    {
        var userId = Guid.NewGuid();
        var gateway = new Mock<ICustomerPasswordResetGateway>();
        gateway.Setup(g => g.FindUserIdByEmailAsync("ok@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);
        gateway.Setup(g => g.IsDisabledAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        gateway.Setup(g => g.TryRequestResetEmailAsync("ok@example.test", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("provider down"));

        var useCase = new RequestCustomerPasswordResetUseCase(
            gateway.Object,
            NullLogger<RequestCustomerPasswordResetUseCase>.Instance);

        var result = await useCase.ExecuteAsync(
            new CustomerPasswordForgotRequest("ok@example.test"));

        Assert.True(result.IsSuccess);
        Assert.Equal(RequestCustomerPasswordResetUseCase.PublicGenericMessage, result.Value.Message);
    }

    [Fact]
    public async Task Forgot_EmptyEmail_FailsValidation()
    {
        var gateway = new Mock<ICustomerPasswordResetGateway>();
        var useCase = new RequestCustomerPasswordResetUseCase(
            gateway.Object,
            NullLogger<RequestCustomerPasswordResetUseCase>.Instance);

        var result = await useCase.ExecuteAsync(new CustomerPasswordForgotRequest("  "));

        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrors.EmailRequired.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Reset_Success_ReturnsSuccess()
    {
        var challengeId = Guid.NewGuid();
        var gateway = new Mock<ICustomerPasswordResetGateway>();
        gateway.Setup(g => g.ResetPasswordAsync(
                challengeId, "token", "New-Pass-1!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerPasswordResetOutcome(true, null));

        var useCase = new ResetCustomerPasswordUseCase(gateway.Object);
        var result = await useCase.ExecuteAsync(
            new CustomerPasswordResetRequest(challengeId, "token", "New-Pass-1!"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Reset_ExpiredToken_MapsError()
    {
        var challengeId = Guid.NewGuid();
        var gateway = new Mock<ICustomerPasswordResetGateway>();
        gateway.Setup(g => g.ResetPasswordAsync(
                challengeId, "token", "New-Pass-1!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerPasswordResetOutcome(false, "Verification.Expired"));

        var useCase = new ResetCustomerPasswordUseCase(gateway.Object);
        var result = await useCase.ExecuteAsync(
            new CustomerPasswordResetRequest(challengeId, "token", "New-Pass-1!"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrors.PasswordResetLinkExpired.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Reset_ConsumedToken_MapsError()
    {
        var challengeId = Guid.NewGuid();
        var gateway = new Mock<ICustomerPasswordResetGateway>();
        gateway.Setup(g => g.ResetPasswordAsync(
                challengeId, "token", "New-Pass-1!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerPasswordResetOutcome(false, "Verification.AlreadyConsumed"));

        var useCase = new ResetCustomerPasswordUseCase(gateway.Object);
        var result = await useCase.ExecuteAsync(
            new CustomerPasswordResetRequest(challengeId, "token", "New-Pass-1!"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrors.PasswordResetLinkUsed.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Reset_InvalidToken_MapsError()
    {
        var challengeId = Guid.NewGuid();
        var gateway = new Mock<ICustomerPasswordResetGateway>();
        gateway.Setup(g => g.ResetPasswordAsync(
                challengeId, "bad", "New-Pass-1!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerPasswordResetOutcome(false, "Verification.InvalidToken"));

        var useCase = new ResetCustomerPasswordUseCase(gateway.Object);
        var result = await useCase.ExecuteAsync(
            new CustomerPasswordResetRequest(challengeId, "bad", "New-Pass-1!"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrors.PasswordResetLinkInvalid.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Reset_InvalidPassword_MapsPolicyError()
    {
        var challengeId = Guid.NewGuid();
        var gateway = new Mock<ICustomerPasswordResetGateway>();
        gateway.Setup(g => g.ResetPasswordAsync(
                challengeId, "token", "weak", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerPasswordResetOutcome(false, "Authentication.InvalidPassword"));

        var useCase = new ResetCustomerPasswordUseCase(gateway.Object);
        var result = await useCase.ExecuteAsync(
            new CustomerPasswordResetRequest(challengeId, "token", "weak"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrors.PasswordPolicyFailed.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Reset_MissingChallenge_FailsLocally()
    {
        var gateway = new Mock<ICustomerPasswordResetGateway>();
        var useCase = new ResetCustomerPasswordUseCase(gateway.Object);

        var result = await useCase.ExecuteAsync(
            new CustomerPasswordResetRequest(Guid.Empty, "token", "New-Pass-1!"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CustomerErrors.PasswordResetLinkInvalid.Code, result.Error!.Code);
        gateway.Verify(
            g => g.ResetPasswordAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Reset_WhenSessionRevocationFails_DoesNotReportSuccess()
    {
        var challengeId = Guid.NewGuid();
        var gateway = new Mock<ICustomerPasswordResetGateway>();
        gateway.Setup(g => g.ResetPasswordAsync(
                challengeId, "token", "New-Pass-1!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerPasswordResetOutcome(
                false,
                "Customer.PasswordResetSessionRevocationFailed"));

        var useCase = new ResetCustomerPasswordUseCase(gateway.Object);
        var result = await useCase.ExecuteAsync(
            new CustomerPasswordResetRequest(challengeId, "token", "New-Pass-1!"));

        Assert.False(result.IsSuccess);
        Assert.Equal(
            CustomerErrors.PasswordResetSessionRevocationFailed.Code,
            result.Error!.Code);
    }
}
