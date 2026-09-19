using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;

namespace ElectricalStore.Application.Media;

public static class MediaErrors
{
    public static Error EmptyFile { get; } =
        new("Media.EmptyFile", "An image file is required.");

    public static Error FileTooLarge { get; } =
        new("Media.FileTooLarge", "The image exceeds the configured maximum upload size.");

    public static Error InvalidContentType { get; } =
        new("Media.InvalidContentType", "The file type is not an allowed image format.");

    public static Error InvalidImageContent { get; } =
        new("Media.InvalidImageContent", "The file content is not a valid image.");

    public static Error UploadFailed { get; } =
        new("Media.UploadFailed", "The image could not be stored. Try again later.");

    public static Error NotConfigured { get; } =
        new("Media.NotConfigured", "Image storage is not configured.");
}

internal static class ImageStorageFailureMapper
{
    public static Error Map(ImageStorageException ex) =>
        ex.Message.Contains("not configured", StringComparison.OrdinalIgnoreCase)
            ? MediaErrors.NotConfigured
            : MediaErrors.UploadFailed;
}

public sealed class ImageUploadValidator
{
    private readonly MediaOptions _options;

    public ImageUploadValidator(MediaOptions options)
    {
        _options = options;
    }

    public Result Validate(Stream content, string? contentType, long? declaredLength)
    {
        if (content is null || !content.CanRead)
            return Result.Failure(MediaErrors.EmptyFile);

        if (declaredLength is 0 or < 0)
            return Result.Failure(MediaErrors.EmptyFile);

        if (declaredLength > _options.MaxImageSizeBytes)
            return Result.Failure(MediaErrors.FileTooLarge);

        if (string.IsNullOrWhiteSpace(contentType)
            || !_options.AllowedContentTypes.Contains(contentType.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            return Result.Failure(MediaErrors.InvalidContentType);
        }

        if (!content.CanSeek)
            return Result.Failure(MediaErrors.InvalidImageContent);

        var original = content.Position;
        Span<byte> header = stackalloc byte[12];
        var read = content.Read(header);
        content.Position = original;

        if (read < 3 || !LooksLikeAllowedImage(header[..read], contentType))
            return Result.Failure(MediaErrors.InvalidImageContent);

        if (content.Length > _options.MaxImageSizeBytes)
            return Result.Failure(MediaErrors.FileTooLarge);

        if (content.Length == 0)
            return Result.Failure(MediaErrors.EmptyFile);

        return Result.Success();
    }

    private static bool LooksLikeAllowedImage(ReadOnlySpan<byte> header, string contentType)
    {
        var isJpeg = header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var isPng = header.Length >= 8
                    && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
                    && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A;
        var isWebp = header.Length >= 12
                     && header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F'
                     && header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P';

        return contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) && isJpeg
               || contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase) && isPng
               || contentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase) && isWebp;
    }
}
