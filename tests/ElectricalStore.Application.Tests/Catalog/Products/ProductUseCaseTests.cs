using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Catalog.Products;
using ElectricalStore.Domain.Catalog.Categories;
using ElectricalStore.Domain.Catalog.Products;
using Moq;
using Xunit;

namespace ElectricalStore.Application.Tests.Catalog.Products;

public sealed class ProductUseCaseTests
{
    private static CreateProductVariantRequest Variant(
        string name = "Standard",
        string sku = "SKU-1",
        decimal price = 10m,
        string unit = "Piece",
        decimal increment = 1m,
        bool isActive = true) =>
        new(name, sku, price, unit, increment, isActive);

    [Fact]
    public async Task Create_Valid_Succeeds()
    {
        var category = Category.Create("Cables", null, true);
        var categories = new Mock<ICategoryRepository>();
        categories.Setup(c => c.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var products = new Mock<IProductRepository>();
        products.Setup(p => p.AnyNormalizedSkuExistsAsync(It.IsAny<IReadOnlyCollection<string>>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        products.Setup(p => p.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var uow = new Mock<IAppUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var useCase = new CreateProductUseCase(products.Object, categories.Object, uow.Object);
        var result = await useCase.ExecuteAsync(new CreateProductRequest(
            "Cable",
            "Desc",
            category.Id,
            true,
            [Variant()]));

        Assert.True(result.IsSuccess);
        Assert.Equal("Cable", result.Value.Name);
        Assert.Single(result.Value.Variants);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_MissingCategory_Fails()
    {
        var categories = new Mock<ICategoryRepository>();
        categories.Setup(c => c.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var products = new Mock<IProductRepository>();
        products.Setup(p => p.AnyNormalizedSkuExistsAsync(It.IsAny<IReadOnlyCollection<string>>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new CreateProductUseCase(products.Object, categories.Object, Mock.Of<IAppUnitOfWork>());
        var result = await useCase.ExecuteAsync(new CreateProductRequest(
            "Cable",
            null,
            Guid.NewGuid(),
            true,
            [Variant()]));

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.CategoryNotFound.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Create_DuplicateSku_Fails()
    {
        var category = Category.Create("Cables", null, true);
        var categories = new Mock<ICategoryRepository>();
        categories.Setup(c => c.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var products = new Mock<IProductRepository>();
        products.Setup(p => p.AnyNormalizedSkuExistsAsync(It.IsAny<IReadOnlyCollection<string>>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new CreateProductUseCase(products.Object, categories.Object, Mock.Of<IAppUnitOfWork>());
        var result = await useCase.ExecuteAsync(new CreateProductRequest(
            "Cable",
            null,
            category.Id,
            true,
            [Variant()]));

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.SkuAlreadyExists.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Create_DuplicateSkuInRequest_Fails()
    {
        var category = Category.Create("Cables", null, true);
        var categories = new Mock<ICategoryRepository>();
        categories.Setup(c => c.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var products = new Mock<IProductRepository>();
        products.Setup(p => p.AnyNormalizedSkuExistsAsync(It.IsAny<IReadOnlyCollection<string>>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new CreateProductUseCase(products.Object, categories.Object, Mock.Of<IAppUnitOfWork>());
        var result = await useCase.ExecuteAsync(new CreateProductRequest(
            "Cable",
            null,
            category.Id,
            true,
            [Variant("A", "sku-1"), Variant("B", "SKU-1")]));

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.DuplicateSkuInRequest.Code, result.Error!.Code);
        products.Verify(
            p => p.AnyNormalizedSkuExistsAsync(It.IsAny<IReadOnlyCollection<string>>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Create_NoVariants_Fails()
    {
        var useCase = new CreateProductUseCase(
            Mock.Of<IProductRepository>(),
            Mock.Of<ICategoryRepository>(),
            Mock.Of<IAppUnitOfWork>());

        var result = await useCase.ExecuteAsync(new CreateProductRequest(
            "Cable",
            null,
            Guid.NewGuid(),
            true,
            []));

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.VariantsRequired.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Create_UniqueConstraintRace_MapsToSkuConflict()
    {
        var category = Category.Create("Cables", null, true);
        var categories = new Mock<ICategoryRepository>();
        categories.Setup(c => c.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var products = new Mock<IProductRepository>();
        products.Setup(p => p.AnyNormalizedSkuExistsAsync(It.IsAny<IReadOnlyCollection<string>>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        products.Setup(p => p.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var uow = new Mock<IAppUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintViolationException("dup"));

        var useCase = new CreateProductUseCase(products.Object, categories.Object, uow.Object);
        var result = await useCase.ExecuteAsync(new CreateProductRequest(
            "Cable",
            null,
            category.Id,
            true,
            [Variant()]));

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.SkuAlreadyExists.Code, result.Error!.Code);
    }
}
