using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Shipping;
using ElectricalStore.Domain.Shipping;
using Moq;
using Xunit;

namespace ElectricalStore.Application.Tests.Shipping;

public sealed class DeliveryZoneUseCaseTests
{
    [Fact]
    public async Task Create_Valid_Succeeds()
    {
        var repo = new Mock<IDeliveryZoneRepository>();
        repo.Setup(r => r.ExistsByNormalizedNameAsync("SULAYMANIYAH", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.AddAsync(It.IsAny<DeliveryZone>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var uow = new Mock<IAppUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var useCase = new CreateDeliveryZoneUseCase(repo.Object, uow.Object);
        var result = await useCase.ExecuteAsync(new CreateDeliveryZoneRequest("Sulaymaniyah", 2500m, true));

        Assert.True(result.IsSuccess);
        Assert.Equal("Sulaymaniyah", result.Value.Name);
        Assert.Equal(2500m, result.Value.Fee);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_ZeroFee_Succeeds()
    {
        var repo = new Mock<IDeliveryZoneRepository>();
        repo.Setup(r => r.ExistsByNormalizedNameAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.AddAsync(It.IsAny<DeliveryZone>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var uow = new Mock<IAppUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var useCase = new CreateDeliveryZoneUseCase(repo.Object, uow.Object);
        var result = await useCase.ExecuteAsync(new CreateDeliveryZoneRequest("Pickup", 0m, true));

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.Fee);
    }

    [Fact]
    public async Task Create_NegativeFee_Fails()
    {
        var useCase = new CreateDeliveryZoneUseCase(
            Mock.Of<IDeliveryZoneRepository>(),
            Mock.Of<IAppUnitOfWork>());

        var result = await useCase.ExecuteAsync(new CreateDeliveryZoneRequest("Bad", -5m, true));

        Assert.True(result.IsFailure);
        Assert.Equal(ShippingErrors.NegativeFee.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Create_DuplicateName_Fails()
    {
        var repo = new Mock<IDeliveryZoneRepository>();
        repo.Setup(r => r.ExistsByNormalizedNameAsync("AZIZIEH", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new CreateDeliveryZoneUseCase(repo.Object, Mock.Of<IAppUnitOfWork>());
        var result = await useCase.ExecuteAsync(new CreateDeliveryZoneRequest("azizieh", 100m, true));

        Assert.True(result.IsFailure);
        Assert.Equal(ShippingErrors.NameAlreadyExists.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Create_UniqueConstraintRace_MapsToConflict()
    {
        var repo = new Mock<IDeliveryZoneRepository>();
        repo.Setup(r => r.ExistsByNormalizedNameAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.AddAsync(It.IsAny<DeliveryZone>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var uow = new Mock<IAppUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintViolationException("dup"));

        var useCase = new CreateDeliveryZoneUseCase(repo.Object, uow.Object);
        var result = await useCase.ExecuteAsync(new CreateDeliveryZoneRequest("Zone", 10m, true));

        Assert.True(result.IsFailure);
        Assert.Equal(ShippingErrors.NameAlreadyExists.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Get_Missing_NotFound()
    {
        var repo = new Mock<IDeliveryZoneRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeliveryZone?)null);

        var useCase = new GetAdminDeliveryZoneByIdUseCase(repo.Object);
        var result = await useCase.ExecuteAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ShippingErrors.NotFound.Code, result.Error!.Code);
    }

    [Fact]
    public async Task ListActive_ReturnsPublicDtosOnly()
    {
        var active = DeliveryZone.Create("A", 100m, true);
        var repo = new Mock<IDeliveryZoneRepository>();
        repo.Setup(r => r.ListAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeliveryZone> { active });

        var useCase = new ListActiveDeliveryZonesUseCase(repo.Object);
        var list = await useCase.ExecuteAsync();

        Assert.Single(list);
        Assert.Equal(active.Id, list[0].Id);
        Assert.Equal(100m, list[0].Fee);
    }
}
