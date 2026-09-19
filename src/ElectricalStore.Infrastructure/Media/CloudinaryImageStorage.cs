using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StoredImage = ElectricalStore.Application.Abstractions.StoredImage;

namespace ElectricalStore.Infrastructure.Media;

public sealed class CloudinaryImageStorage : IImageStorage
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryImageStorage> _logger;

    public CloudinaryImageStorage(IOptions<CloudinaryOptions> options, ILogger<CloudinaryImageStorage> logger)
    {
        _logger = logger;
        var value = options.Value;
        if (!value.IsConfigured)
            throw new InvalidOperationException("Cloudinary is not configured.");

        _cloudinary = new Cloudinary(new Account(value.CloudName, value.ApiKey, value.ApiSecret));
        _cloudinary.Api.Secure = true;
    }

    public async Task<StoredImage> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(string.IsNullOrWhiteSpace(fileName) ? "upload" : fileName, content),
                Folder = folder,
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false
            };

            var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
            // Cloudinary public_id maps to the provider-neutral StorageKey.
            if (result.Error is not null || string.IsNullOrWhiteSpace(result.PublicId) || result.SecureUrl is null)
            {
                _logger.LogError(
                    "Cloudinary upload rejected. Operation={Operation} Provider={Provider} StatusCode={StatusCode}",
                    "Upload",
                    "Cloudinary",
                    result.StatusCode);
                throw new ImageStorageException("Cloudinary upload failed.");
            }

            return new StoredImage(result.PublicId, result.SecureUrl.ToString());
        }
        catch (ImageStorageException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cloudinary upload threw. Operation={Operation} Provider={Provider}", "Upload", "Cloudinary");
            throw new ImageStorageException("Cloudinary upload failed.", ex);
        }
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return;

        try
        {
            // StorageKey is the Cloudinary public_id for this provider.
            var result = await _cloudinary.DestroyAsync(new DeletionParams(storageKey)
            {
                ResourceType = ResourceType.Image
            });

            if (result.Error is not null)
            {
                _logger.LogWarning(
                    "Cloudinary delete reported error. Operation={Operation} Provider={Provider} StatusCode={StatusCode}",
                    "Delete",
                    "Cloudinary",
                    result.StatusCode);
                throw new ImageStorageException("Cloudinary delete failed.");
            }
        }
        catch (ImageStorageException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cloudinary delete threw. Operation={Operation} Provider={Provider}", "Delete", "Cloudinary");
            throw new ImageStorageException("Cloudinary delete failed.", ex);
        }
    }
}

public sealed class UnconfiguredImageStorage : IImageStorage
{
    public Task<StoredImage> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken cancellationToken = default) =>
        throw new ImageStorageException("Image storage is not configured.");

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default) =>
        throw new ImageStorageException("Image storage is not configured.");
}

/// <summary>In-memory image storage for automated tests (no network / credentials).</summary>
public sealed class FakeImageStorage : IImageStorage
{
    private readonly Dictionary<string, string> _assets = new(StringComparer.Ordinal);
    public bool FailNextUpload { get; set; }
    public bool FailNextDelete { get; set; }
    public int UploadCount { get; private set; }
    public int DeleteCount { get; private set; }
    public IReadOnlyDictionary<string, string> Assets => _assets;

    public Task<StoredImage> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken cancellationToken = default)
    {
        UploadCount++;
        if (FailNextUpload)
        {
            FailNextUpload = false;
            throw new ImageStorageException("Simulated upload failure.");
        }

        var storageKey = $"{folder.Trim('/')}/{Guid.NewGuid():N}";
        var url = $"https://img.test/{storageKey}.jpg";
        _assets[storageKey] = url;
        return Task.FromResult(new StoredImage(storageKey, url));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        DeleteCount++;
        if (FailNextDelete)
        {
            FailNextDelete = false;
            throw new ImageStorageException("Simulated delete failure.");
        }

        _assets.Remove(storageKey);
        return Task.CompletedTask;
    }
}
