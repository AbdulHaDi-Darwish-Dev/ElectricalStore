using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;
using ElectricalStore.Application.Customers;
using ElectricalStore.Domain.Customers;
using Moq;
using Xunit;

namespace ElectricalStore.Application.Tests.Customers;

public sealed class CustomerProfileUseCaseTests
{
    [Theory]
    [InlineData("abdul@example.test", "abdul")]
    [InlineData("User.Name", "User.Name")]
    public void DeriveProvisionalFullName_UsesEmailLocalPart(string email, string expected)
    {
        Assert.Equal(expected, GetCustomerProfileUseCase.DeriveProvisionalFullName(email, "ignored"));
    }

    [Fact]
    public void ValidateFullName_RejectsEmpty()
    {
        var result = UpdateCustomerProfileUseCase.ValidateFullName("  ");
        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.FullNameRequired.Code, result.Error!.Code);
    }

    [Fact]
    public void TechnicalUserName_IsIdentitySafeAndUnique()
    {
        var a = CustomerTechnicalUserName.Create();
        var b = CustomerTechnicalUserName.Create();
        Assert.NotEqual(a, b);
        Assert.StartsWith("customer-", a);
        Assert.Matches(@"^customer-[0-9a-f]{32}$", a);
        Assert.DoesNotContain("@", a);
        Assert.DoesNotContain(".", a);
    }

    [Fact]
    public async Task GetProfile_MissingRow_ReturnsFallbackWithoutPersisting()
    {
        var userId = Guid.NewGuid();
        var profiles = new Mock<ICustomerProfileRepository>();
        profiles.Setup(p => p.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerProfile?)null);

        var identity = new Mock<ICustomerIdentityLookup>();
        identity.Setup(i => i.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerIdentityInfo(userId, "legacy-user@example.test", false, "legacyuser"));

        var useCase = new GetCustomerProfileUseCase(profiles.Object, identity.Object);
        var result = await useCase.ExecuteAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.Equal("legacy-user", result.Value.FullName);
        Assert.Equal("legacy-user@example.test", result.Value.Email);
        profiles.Verify(
            p => p.AddAsync(It.IsAny<CustomerProfile>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateProfile_MissingRow_CreatesProfile()
    {
        var userId = Guid.NewGuid();
        var profiles = new Mock<ICustomerProfileRepository>();
        profiles.Setup(p => p.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerProfile?)null);
        profiles.Setup(p => p.AddAsync(It.IsAny<CustomerProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var identity = new Mock<ICustomerIdentityLookup>();
        identity.Setup(i => i.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerIdentityInfo(userId, "legacy@example.test", true, "legacy"));

        var uow = new Mock<IAppUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var clock = new Mock<IAppClock>();
        clock.SetupGet(c => c.UtcNow).Returns(new DateTime(2026, 9, 22, 12, 0, 0, DateTimeKind.Utc));

        var useCase = new UpdateCustomerProfileUseCase(
            profiles.Object,
            identity.Object,
            uow.Object,
            clock.Object);

        var result = await useCase.ExecuteAsync(
            userId,
            new UpdateCustomerProfileRequest("عبدالهادي درويش"));

        Assert.True(result.IsSuccess);
        Assert.Equal("عبدالهادي درويش", result.Value.FullName);
        profiles.Verify(
            p => p.AddAsync(It.IsAny<CustomerProfile>(), It.IsAny<CancellationToken>()),
            Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteRegistration_ProfileFailure_CompensatesSuccessfully()
    {
        var userId = Guid.NewGuid();
        var profiles = new Mock<ICustomerProfileRepository>();
        profiles.Setup(p => p.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerProfile?)null);
        profiles.Setup(p => p.AddAsync(It.IsAny<CustomerProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var uow = new Mock<IAppUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("forced profile failure"));

        var clock = new Mock<IAppClock>();
        clock.SetupGet(c => c.UtcNow).Returns(DateTime.UtcNow);

        var createProfile = new CreateCustomerProfileForUserUseCase(
            profiles.Object,
            uow.Object,
            clock.Object);

        var compensation = new Mock<ICustomerIdentityCompensation>();
        compensation.Setup(c => c.TryDeleteUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new CompleteCustomerRegistrationUseCase(createProfile, compensation.Object);
        var result = await useCase.ExecuteAsync(userId, "a@example.test", "Test User");

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.ProfileCreateFailed.Code, result.Error!.Code);
        compensation.Verify(c => c.TryDeleteUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.DoesNotContain("forced profile failure", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompleteRegistration_ProfileFailure_CompensationFails_ReturnsCompensationFailed()
    {
        var userId = Guid.NewGuid();
        var profiles = new Mock<ICustomerProfileRepository>();
        profiles.Setup(p => p.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerProfile?)null);
        profiles.Setup(p => p.AddAsync(It.IsAny<CustomerProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var uow = new Mock<IAppUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("forced profile failure"));

        var clock = new Mock<IAppClock>();
        clock.SetupGet(c => c.UtcNow).Returns(DateTime.UtcNow);

        var createProfile = new CreateCustomerProfileForUserUseCase(
            profiles.Object,
            uow.Object,
            clock.Object);

        var compensation = new Mock<ICustomerIdentityCompensation>();
        compensation.Setup(c => c.TryDeleteUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new CompleteCustomerRegistrationUseCase(createProfile, compensation.Object);
        var result = await useCase.ExecuteAsync(userId, "a@example.test", "Test User");

        Assert.True(result.IsFailure);
        Assert.Equal(CustomerErrors.RegistrationCompensationFailed.Code, result.Error!.Code);
        Assert.DoesNotContain("forced profile failure", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }
}
