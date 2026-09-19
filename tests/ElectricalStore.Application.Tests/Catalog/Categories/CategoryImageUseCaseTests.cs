using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Catalog.Categories;
using ElectricalStore.Application.Media;
using ElectricalStore.Domain.Catalog.Categories;
using Moq;
using Xunit;

namespace ElectricalStore.Application.Tests.Catalog.Categories;

public sealed class CategoryImageUseCaseTests
{
    private static readonly byte[] Jpeg =
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
        0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xD9
    ];

    [Fact]
    public async Task Upsert_WhenDbFailsAfterUpload_DeletesUploadedAsset()
    {
        var category = Category.Create("With Image", null, true);
        var categories = new Mock<ICategoryRepository>();
        categories.Setup(r => r.GetTrackedByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var unitOfWork = new Mock<IAppUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var storage = new Mock<IImageStorage>();
        storage.Setup(s => s.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredImage("new/public", "https://cdn.test/new.jpg"));

        var useCase = new UpsertCategoryImageUseCase(
            categories.Object,
            unitOfWork.Object,
            storage.Object,
            new ImageUploadValidator(new MediaOptions()));

        await using var stream = new MemoryStream(Jpeg);
        var result = await useCase.ExecuteAsync(
            category.Id,
            stream,
            "cat.jpg",
            "image/jpeg",
            Jpeg.Length);

        Assert.True(result.IsFailure);
        Assert.Equal(MediaErrors.UploadFailed.Code, result.Error!.Code);
        storage.Verify(
            s => s.DeleteAsync("new/public", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Upsert_WhenStorageFails_ReturnsUploadFailed()
    {
        var category = Category.Create("X", null, true);
        var categories = new Mock<ICategoryRepository>();
        categories.Setup(r => r.GetTrackedByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var storage = new Mock<IImageStorage>();
        storage.Setup(s => s.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ImageStorageException("Cloudinary upload failed."));

        var useCase = new UpsertCategoryImageUseCase(
            categories.Object,
            Mock.Of<IAppUnitOfWork>(),
            storage.Object,
            new ImageUploadValidator(new MediaOptions()));

        await using var stream = new MemoryStream(Jpeg);
        var result = await useCase.ExecuteAsync(
            category.Id,
            stream,
            "cat.jpg",
            "image/jpeg",
            Jpeg.Length);

        Assert.True(result.IsFailure);
        Assert.Equal(MediaErrors.UploadFailed.Code, result.Error!.Code);
    }
}
