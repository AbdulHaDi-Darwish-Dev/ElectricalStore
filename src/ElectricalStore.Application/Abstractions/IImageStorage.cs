namespace ElectricalStore.Application.Abstractions;

public sealed record StoredImage(string StorageKey, string Url);

/// <summary>Application-owned image storage port. Infrastructure provides the concrete provider (or test fakes).</summary>
public interface IImageStorage
{
    Task<StoredImage> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}

public sealed class ImageStorageException : Exception
{
    public ImageStorageException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
