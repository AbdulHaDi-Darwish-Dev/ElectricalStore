using Moq;
using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Catalog.Categories;
using ElectricalStore.Domain.Catalog.Categories;
using Xunit;

namespace ElectricalStore.Application.Tests.Catalog.Categories;

public sealed class CategoryUseCaseTests
{
    [Fact]
    public async Task Create_Valid_Succeeds()
    {
        var repo = new Mock<ICategoryRepository>();
        repo.Setup(r => r.ExistsByNormalizedNameAsync("CABLES", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var uow = new Mock<IAppUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var useCase = new CreateCategoryUseCase(repo.Object, uow.Object);
        var result = await useCase.ExecuteAsync(new CreateCategoryRequest
        {
            Name = "Cables",
            Description = "Wire",
            IsActive = true
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Cables", result.Value.Name);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_DuplicateNormalizedName_Fails()
    {
        var repo = new Mock<ICategoryRepository>();
        repo.Setup(r => r.ExistsByNormalizedNameAsync("CABLES", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new CreateCategoryUseCase(repo.Object, Mock.Of<IAppUnitOfWork>());
        var result = await useCase.ExecuteAsync(new CreateCategoryRequest
        {
            Name = "cables",
            IsActive = true
        });

        Assert.True(result.IsFailure);
        Assert.Equal(CategoryErrors.NameAlreadyExists.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Update_MissingCategory_NotFound()
    {
        var repo = new Mock<ICategoryRepository>();
        repo.Setup(r => r.GetTrackedByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var useCase = new UpdateCategoryUseCase(repo.Object, Mock.Of<IAppUnitOfWork>());
        var result = await useCase.ExecuteAsync(Guid.NewGuid(), new UpdateCategoryRequest
        {
            Name = "Anything"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(CategoryErrors.NotFound.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Update_DuplicateName_Conflict()
    {
        var existing = Category.Create("Tools", null, true);
        var repo = new Mock<ICategoryRepository>();
        repo.Setup(r => r.GetTrackedByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        repo.Setup(r => r.ExistsByNormalizedNameAsync("CABLES", existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new UpdateCategoryUseCase(repo.Object, Mock.Of<IAppUnitOfWork>());
        var result = await useCase.ExecuteAsync(existing.Id, new UpdateCategoryRequest
        {
            Name = "CABLES"
        });

        Assert.True(result.IsFailure);
        Assert.Equal(CategoryErrors.NameAlreadyExists.Code, result.Error!.Code);
    }

    [Fact]
    public async Task Update_Valid_Succeeds()
    {
        var existing = Category.Create("Tools", null, true);
        var repo = new Mock<ICategoryRepository>();
        repo.Setup(r => r.GetTrackedByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        repo.Setup(r => r.ExistsByNormalizedNameAsync("SWITCHES", existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var uow = new Mock<IAppUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var useCase = new UpdateCategoryUseCase(repo.Object, uow.Object);
        var result = await useCase.ExecuteAsync(existing.Id, new UpdateCategoryRequest
        {
            Name = "Switches",
            Description = "Electrical switches"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Switches", result.Value.Name);
        Assert.Equal("Electrical switches", result.Value.Description);
    }

    [Fact]
    public async Task ListActive_ReturnsActiveOnly()
    {
        var repo = new Mock<ICategoryRepository>();
        repo.Setup(r => r.ListAsync(true, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>
            {
                Category.Create("Active", null, true)
            });

        var useCase = new ListActiveCategoriesUseCase(repo.Object);
        var list = await useCase.ExecuteAsync();

        Assert.Single(list);
        Assert.All(list, c => Assert.True(c.IsActive));
        repo.Verify(r => r.ListAsync(true, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAdmin_IncludesInactive()
    {
        var repo = new Mock<ICategoryRepository>();
        repo.Setup(r => r.ListAsync(false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>
            {
                Category.Create("Active", null, true),
                Category.Create("Inactive", null, false)
            });

        var useCase = new ListAdminCategoriesUseCase(repo.Object);
        var list = await useCase.ExecuteAsync();

        Assert.Equal(2, list.Count);
        repo.Verify(r => r.ListAsync(false, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetActive_Inactive_NotFound()
    {
        var inactive = Category.Create("Hidden", null, false);
        var repo = new Mock<ICategoryRepository>();
        repo.Setup(r => r.GetByIdAsync(inactive.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactive);

        var useCase = new GetActiveCategoryByIdUseCase(repo.Object);
        var result = await useCase.ExecuteAsync(inactive.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(CategoryErrors.NotFound.Code, result.Error!.Code);
    }

    [Fact]
    public async Task GetActive_ActiveWithoutImage_NotFound()
    {
        var active = Category.Create("No Image", null, true);
        var repo = new Mock<ICategoryRepository>();
        repo.Setup(r => r.GetByIdAsync(active.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(active);

        var useCase = new GetActiveCategoryByIdUseCase(repo.Object);
        var result = await useCase.ExecuteAsync(active.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(CategoryErrors.NotFound.Code, result.Error!.Code);
    }

    [Fact]
    public async Task GetAdmin_Inactive_Succeeds()
    {
        var inactive = Category.Create("Hidden", null, false);
        var repo = new Mock<ICategoryRepository>();
        repo.Setup(r => r.GetByIdAsync(inactive.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactive);

        var useCase = new GetAdminCategoryByIdUseCase(repo.Object);
        var result = await useCase.ExecuteAsync(inactive.Id);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);
    }
}
