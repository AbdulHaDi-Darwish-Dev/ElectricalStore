namespace ElectricalStore.Application.Media;

public sealed class MediaOptions
{
    public const string SectionName = "Media";

    /// <summary>Maximum accepted upload size for NEW uploads only. Does not affect existing assets.</summary>
    public int MaxImageSizeMb { get; set; } = 5;

    public string[] AllowedContentTypes { get; set; } =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    /// <summary>
    /// When true (Development only), use in-memory image storage if Cloudinary is not configured.
    /// Never enable in Production.
    /// </summary>
    public bool AllowLocalDevStorage { get; set; }

    public long MaxImageSizeBytes => Math.Max(1, MaxImageSizeMb) * 1024L * 1024L;
}
