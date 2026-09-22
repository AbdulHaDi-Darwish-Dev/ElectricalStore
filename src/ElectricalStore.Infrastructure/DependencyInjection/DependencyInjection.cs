using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Catalog.Categories;
using ElectricalStore.Application.Catalog.Products;
using ElectricalStore.Application.Customers;
using ElectricalStore.Application.Inventory;
using ElectricalStore.Application.Media;
using ElectricalStore.Application.Ordering;
using ElectricalStore.Application.Shipping;
using ElectricalStore.Infrastructure.Media;
using ElectricalStore.Infrastructure.Ordering;
using ElectricalStore.Infrastructure.Persistence;
using ElectricalStore.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ElectricalStore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAppInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("A SQL Server connection string is required for AppDbContext.");

        services.AddDbContext<AppDbContext>(options =>
        {
            // Never enable EF sensitive-data logging (parameter values) in any environment.
            options.EnableSensitiveDataLogging(false);
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsHistoryTable(AppDbContext.MigrationsHistoryTable));
        });

        var mediaOptions = new MediaOptions();
        configuration.GetSection(MediaOptions.SectionName).Bind(mediaOptions);
        services.AddSingleton(mediaOptions);
        services.AddSingleton<ImageUploadValidator>();

        var isProduction = string.Equals(
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            Environments.Production,
            StringComparison.OrdinalIgnoreCase);

        var cloudinaryOptions = new CloudinaryOptions();
        configuration.GetSection(CloudinaryOptions.SectionName).Bind(cloudinaryOptions);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(cloudinaryOptions));

        if (cloudinaryOptions.IsConfigured)
            services.AddSingleton<IImageStorage, CloudinaryImageStorage>();
        else if (mediaOptions.AllowLocalDevStorage && !isProduction)
            services.AddSingleton<IImageStorage, FakeImageStorage>();
        else
            services.AddSingleton<IImageStorage, UnconfiguredImageStorage>();

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IDeliveryZoneRepository, DeliveryZoneRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderCatalogQuery, OrderCatalogQuery>();
        services.AddScoped<IOrderingSettingsRepository, OrderingSettingsRepository>();
        services.AddScoped<IOrderPlacementIdempotencyRepository, OrderPlacementIdempotencyRepository>();
        services.AddSingleton<IGuestOrderTokenService, GuestOrderTokenService>();

        // Persist keys so guest Place Order idempotency can Unprotect across process restarts.
        // Production MUST set DataProtection:KeysPath to a durable mounted volume.
        var keysPath = configuration["DataProtection:KeysPath"];
        if (string.IsNullOrWhiteSpace(keysPath))
        {
            if (isProduction)
            {
                throw new InvalidOperationException(
                    "Production requires DataProtection:KeysPath (durable volume). " +
                    "Refusing ephemeral BaseDirectory keys.");
            }

            keysPath = Path.Combine(AppContext.BaseDirectory, "dp-keys");
        }

        Directory.CreateDirectory(keysPath);
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
            .SetApplicationName("ElectricalStore");
        services.AddSingleton<IGuestAccessTokenProtector, DataProtectionGuestAccessTokenProtector>();
        services.AddScoped<IAppUnitOfWork, AppUnitOfWork>();
        services.AddSingleton<IAppClock, SystemAppClock>();

        services.AddScoped<CreateCategoryUseCase>();
        services.AddScoped<UpdateCategoryUseCase>();
        services.AddScoped<ActivateCategoryUseCase>();
        services.AddScoped<DeactivateCategoryUseCase>();
        services.AddScoped<GetAdminCategoryByIdUseCase>();
        services.AddScoped<ListAdminCategoriesUseCase>();
        services.AddScoped<GetActiveCategoryByIdUseCase>();
        services.AddScoped<ListActiveCategoriesUseCase>();
        services.AddScoped<UpsertCategoryImageUseCase>();
        services.AddScoped<DeleteCategoryImageUseCase>();

        services.AddScoped<CreateProductUseCase>();
        services.AddScoped<UpdateProductUseCase>();
        services.AddScoped<ActivateProductUseCase>();
        services.AddScoped<DeactivateProductUseCase>();
        services.AddScoped<AddProductVariantUseCase>();
        services.AddScoped<UpdateProductVariantUseCase>();
        services.AddScoped<ActivateProductVariantUseCase>();
        services.AddScoped<DeactivateProductVariantUseCase>();
        services.AddScoped<GetAdminProductByIdUseCase>();
        services.AddScoped<ListAdminProductsUseCase>();
        services.AddScoped<GetCatalogProductByIdUseCase>();
        services.AddScoped<ListCatalogProductsUseCase>();
        services.AddScoped<AddProductImageUseCase>();
        services.AddScoped<DeleteProductImageUseCase>();
        services.AddScoped<SetPrimaryProductImageUseCase>();
        services.AddScoped<ReorderProductImagesUseCase>();

        services.AddScoped<AdjustInventoryUseCase>();
        services.AddScoped<ListAdminInventoryUseCase>();
        services.AddScoped<GetAdminInventoryByVariantIdUseCase>();
        services.AddScoped<ListInventoryAdjustmentsUseCase>();

        services.AddScoped<CreateDeliveryZoneUseCase>();
        services.AddScoped<UpdateDeliveryZoneUseCase>();
        services.AddScoped<ActivateDeliveryZoneUseCase>();
        services.AddScoped<DeactivateDeliveryZoneUseCase>();
        services.AddScoped<GetAdminDeliveryZoneByIdUseCase>();
        services.AddScoped<ListAdminDeliveryZonesUseCase>();
        services.AddScoped<ListActiveDeliveryZonesUseCase>();

        services.AddScoped<ICustomerProfileRepository, CustomerProfileRepository>();
        services.AddScoped<GetCustomerProfileUseCase>();
        services.AddScoped<UpdateCustomerProfileUseCase>();
        services.AddScoped<CreateCustomerProfileForUserUseCase>();
        services.AddScoped<CompleteCustomerRegistrationUseCase>();

        services.AddScoped<CheckoutPricingService>();
        services.AddScoped<CheckoutPreviewUseCase>();
        services.AddScoped<PlaceOrderUseCase>();
        services.AddScoped<ConfirmOrderUseCase>();
        services.AddScoped<PrepareOrderUseCase>();
        services.AddScoped<OutForDeliveryOrderUseCase>();
        services.AddScoped<DeliverOrderUseCase>();
        services.AddScoped<MarkOrderPaidUseCase>();
        services.AddScoped<CancelOrderUseCase>();
        services.AddScoped<ModifyPendingOrderUseCase>();
        services.AddScoped<GetOrderByIdUseCase>();
        services.AddScoped<ListCustomerOrdersUseCase>();
        services.AddScoped<ListAdminOrdersUseCase>();
        services.AddScoped<GetOrderingSettingsUseCase>();
        services.AddScoped<UpdateOrderingSettingsUseCase>();

        return services;
    }
}
