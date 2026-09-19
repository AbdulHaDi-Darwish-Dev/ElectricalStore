using ElectricalStore.Application.Media;
using Xunit;

namespace ElectricalStore.Application.Tests.Media;

public sealed class ImageUploadValidatorTests
{
    private static readonly byte[] Jpeg =
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
        0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xD9
    ];

    [Fact]
    public void AcceptsValidJpegWithinLimit()
    {
        var validator = new ImageUploadValidator(new MediaOptions { MaxImageSizeMb = 5 });
        using var stream = new MemoryStream(Jpeg);

        var result = validator.Validate(stream, "image/jpeg", Jpeg.Length);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void RejectsOversizedDeclaredLength()
    {
        var validator = new ImageUploadValidator(new MediaOptions { MaxImageSizeMb = 1 });
        using var stream = new MemoryStream(Jpeg);

        var result = validator.Validate(stream, "image/jpeg", 2 * 1024 * 1024);

        Assert.True(result.IsFailure);
        Assert.Equal(MediaErrors.FileTooLarge.Code, result.Error!.Code);
    }

    [Fact]
    public void RejectsInvalidContentType()
    {
        var validator = new ImageUploadValidator(new MediaOptions());
        using var stream = new MemoryStream(Jpeg);

        var result = validator.Validate(stream, "application/pdf", Jpeg.Length);

        Assert.True(result.IsFailure);
        Assert.Equal(MediaErrors.InvalidContentType.Code, result.Error!.Code);
    }

    [Fact]
    public void RejectsMismatchedMagicBytes()
    {
        var validator = new ImageUploadValidator(new MediaOptions());
        using var stream = new MemoryStream("not-an-image"u8.ToArray());

        var result = validator.Validate(stream, "image/jpeg", 12);

        Assert.True(result.IsFailure);
        Assert.Equal(MediaErrors.InvalidImageContent.Code, result.Error!.Code);
    }
}
