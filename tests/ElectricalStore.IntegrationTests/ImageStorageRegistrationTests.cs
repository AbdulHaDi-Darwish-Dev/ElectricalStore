using ElectricalStore.Application.Abstractions;
using ElectricalStore.Infrastructure;
using ElectricalStore.Infrastructure.Media;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElectricalStore.IntegrationTests;

public sealed class ImageStorageRegistrationTests
{
    [Fact]
    public void Production_WithAllowLocalDevStorage_DoesNotUseFakeImageStorage()
    {
        using var _ = new EnvironmentOverride("ASPNETCORE_ENVIRONMENT", "Production");
        var keysPath = Path.Combine(Path.GetTempPath(), "es-dp-" + Guid.NewGuid().ToString("N"));
        try
        {
            var sp = BuildProvider(new Dictionary<string, string?>
            {
                ["Media:AllowLocalDevStorage"] = "true",
                ["Media:MaxImageSizeMb"] = "5",
                ["Cloudinary:CloudName"] = "",
                ["Cloudinary:ApiKey"] = "",
                ["Cloudinary:ApiSecret"] = "",
                ["DataProtection:KeysPath"] = keysPath,
            });

            var storage = sp.GetRequiredService<IImageStorage>();
            Assert.IsType<UnconfiguredImageStorage>(storage);
            Assert.IsNotType<FakeImageStorage>(storage);
        }
        finally
        {
            if (Directory.Exists(keysPath))
                Directory.Delete(keysPath, recursive: true);
        }
    }

    [Fact]
    public void Development_WithAllowLocalDevStorage_UsesFakeWhenCloudinaryMissing()
    {
        using var _ = new EnvironmentOverride("ASPNETCORE_ENVIRONMENT", "Development");
        var keysPath = Path.Combine(Path.GetTempPath(), "es-dp-" + Guid.NewGuid().ToString("N"));
        try
        {
            var sp = BuildProvider(new Dictionary<string, string?>
            {
                ["Media:AllowLocalDevStorage"] = "true",
                ["Media:MaxImageSizeMb"] = "5",
                ["Cloudinary:CloudName"] = "",
                ["Cloudinary:ApiKey"] = "",
                ["Cloudinary:ApiSecret"] = "",
                ["DataProtection:KeysPath"] = keysPath,
            });

            Assert.IsType<FakeImageStorage>(sp.GetRequiredService<IImageStorage>());
        }
        finally
        {
            if (Directory.Exists(keysPath))
                Directory.Delete(keysPath, recursive: true);
        }
    }

    [Fact]
    public void Development_WithoutAllowLocalDevStorage_UsesUnconfiguredWhenCloudinaryMissing()
    {
        using var _ = new EnvironmentOverride("ASPNETCORE_ENVIRONMENT", "Development");
        var keysPath = Path.Combine(Path.GetTempPath(), "es-dp-" + Guid.NewGuid().ToString("N"));
        try
        {
            var sp = BuildProvider(new Dictionary<string, string?>
            {
                ["Media:AllowLocalDevStorage"] = "false",
                ["Media:MaxImageSizeMb"] = "5",
                ["Cloudinary:CloudName"] = "",
                ["Cloudinary:ApiKey"] = "",
                ["Cloudinary:ApiSecret"] = "",
                ["DataProtection:KeysPath"] = keysPath,
            });

            Assert.IsType<UnconfiguredImageStorage>(sp.GetRequiredService<IImageStorage>());
        }
        finally
        {
            if (Directory.Exists(keysPath))
                Directory.Delete(keysPath, recursive: true);
        }
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> values)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddAppInfrastructure(
            "Server=(localdb)\\mssqllocaldb;Database=ElectricalStore.ImageStorageRegTest;Trusted_Connection=True;TrustServerCertificate=True",
            config);
        return services.BuildServiceProvider();
    }

    private sealed class EnvironmentOverride : IDisposable
    {
        private readonly string _name;
        private readonly string? _previous;

        public EnvironmentOverride(string name, string value)
        {
            _name = name;
            _previous = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }

        public void Dispose() => Environment.SetEnvironmentVariable(_name, _previous);
    }
}
