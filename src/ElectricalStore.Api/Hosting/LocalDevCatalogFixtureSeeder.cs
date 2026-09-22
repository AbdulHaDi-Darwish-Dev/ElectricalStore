using ElectricalStore.Domain.Catalog.Categories;
using ElectricalStore.Domain.Catalog.Products;
using ElectricalStore.Domain.Inventory;
using ElectricalStore.Domain.Ordering;
using ElectricalStore.Domain.Shipping;
using ElectricalStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ElectricalStore.Api.Hosting;

/// <summary>
/// Idempotent Development catalog fixtures for E2E (category, product, stock, shipping zone).
/// </summary>
public sealed class LocalDevCatalogFixtureSeeder
{
    private const string CategoryName = "E2E Category";
    private const string CategoryImageKey = "local-dev/e2e-category";
    private const string CategoryImageUrl = "https://picsum.photos/seed/e2e-category/640/480";

    private const string ProductName = "E2E Product";
    private const string ProductImageKey = "local-dev/e2e-product";
    private const string ProductImageUrl = "https://picsum.photos/seed/e2e-product/640/480";

    private const string VariantName = "Standard";
    private const string VariantSku = "E2E-STD-001";
    private const decimal VariantPrice = 25m;

    private const string DeliveryZoneName = "E2E Shipping Zone";
    private const decimal DeliveryZoneFee = 5m;

    private const decimal TargetOnHand = 100m;
    private const decimal MinimumAvailableBeforeTopUp = 10m;

    private readonly AppDbContext _db;
    private readonly LocalDevFixtureOptions _options;
    private readonly ILogger<LocalDevCatalogFixtureSeeder> _logger;

    public LocalDevCatalogFixtureSeeder(
        AppDbContext db,
        IOptions<LocalDevFixtureOptions> options,
        ILogger<LocalDevCatalogFixtureSeeder> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return;

        var created = new List<string>();
        var skipped = new List<string>();

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Name == CategoryName, cancellationToken);
        if (category is null)
        {
            category = Category.Create(CategoryName, null, true);
            category.SetImage(CategoryImageKey, CategoryImageUrl);
            _db.Categories.Add(category);
            created.Add($"category '{CategoryName}'");
        }
        else
        {
            skipped.Add($"category '{CategoryName}'");
        }

        var product = await _db.Products
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Name == ProductName, cancellationToken);

        // Recover when SKU already exists under a differently named product (partial prior seed).
        if (product is null)
        {
            var existingBySku = await _db.ProductVariants
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Sku == VariantSku, cancellationToken);
            if (existingBySku is not null)
            {
                product = await _db.Products
                    .Include(p => p.Variants)
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Id == existingBySku.ProductId, cancellationToken);
                if (product is not null)
                    skipped.Add($"product recovered via SKU '{VariantSku}'");
            }
        }

        // Fixture identity is by public catalog name. If SKU recovery found another product,
        // rename/re-home it onto the E2E category so Playwright name selectors keep working.
        if (product is not null
            && (!string.Equals(product.Name, ProductName, StringComparison.Ordinal)
                || product.CategoryId != category.Id))
        {
            product.Update(ProductName, product.Description, category.Id);
            created.Add($"aligned recovered product to fixture name '{ProductName}'");
        }

        ProductVariant? variant;
        if (product is null)
        {
            product = Product.Create(
                ProductName,
                null,
                category.Id,
                true,
                [new ProductVariantSeed(
                    VariantName,
                    VariantSku,
                    VariantPrice,
                    SellingUnit.Piece,
                    1m,
                    true)]);
            product.AddImage(ProductImageKey, ProductImageUrl);
            _db.Products.Add(product);
            variant = product.Variants.Single();
            created.Add($"product '{ProductName}' with variant SKU '{VariantSku}'");
        }
        else
        {
            skipped.Add($"product '{ProductName}'");
            if (!product.HasImages)
            {
                product.AddImage(ProductImageKey, ProductImageUrl);
                created.Add($"product image on existing '{ProductName}'");
            }

            if (!product.IsActive)
            {
                product.Activate();
                created.Add($"reactivated product '{ProductName}'");
            }

            variant = product.Variants.FirstOrDefault(v =>
                string.Equals(v.Sku, VariantSku, StringComparison.Ordinal));
            if (variant is null)
            {
                var variantBySku = await _db.ProductVariants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Sku == VariantSku, cancellationToken);
                if (variantBySku is not null)
                {
                    skipped.Add($"variant SKU '{VariantSku}' (belongs to another product)");
                    variant = variantBySku;
                }
                else
                {
                    variant = product.AddVariant(new ProductVariantSeed(
                        VariantName,
                        VariantSku,
                        VariantPrice,
                        SellingUnit.Piece,
                        1m,
                        true));
                    created.Add($"variant SKU '{VariantSku}' on existing product");
                }
            }
            else
            {
                skipped.Add($"variant SKU '{VariantSku}'");
            }
        }

        if (category is not null && !category.HasImage)
        {
            category.SetImage(CategoryImageKey, CategoryImageUrl);
            created.Add($"category image on existing '{CategoryName}'");
        }
        else if (category is not null
                 && category.HasImage
                 && category.ImageUrl is not null
                 && category.ImageUrl.Contains("img.test", StringComparison.OrdinalIgnoreCase))
        {
            category.SetImage(CategoryImageKey, CategoryImageUrl);
            created.Add($"category image URL refreshed for '{CategoryName}'");
        }

        if (category is not null && !category.IsActive)
        {
            category.Activate();
            created.Add($"reactivated category '{CategoryName}'");
        }

        if (product is not null && product.HasImages)
        {
            var needsRefresh = product.Images.Any(i =>
                i.Url.Contains("img.test", StringComparison.OrdinalIgnoreCase)
                || product.Images.Count(img => img.IsPrimary) != 1);

            if (needsRefresh)
            {
                foreach (var image in product.Images.ToList())
                    product.RemoveImage(image.Id);

                product.AddImage(ProductImageKey, ProductImageUrl);
                created.Add($"product images reset for '{ProductName}'");
            }
        }

        var inventory = await _db.InventoryItems
            .FirstOrDefaultAsync(i => i.ProductVariantId == variant.Id, cancellationToken);
        if (inventory is null)
        {
            inventory = InventoryItem.CreateZero(variant.Id);
            inventory.AdjustOnHand(TargetOnHand);
            _db.InventoryItems.Add(inventory);
            created.Add($"inventory for SKU '{VariantSku}' ({TargetOnHand} on hand)");
        }
        else if (inventory.Available < MinimumAvailableBeforeTopUp)
        {
            var delta = TargetOnHand - inventory.Available;
            inventory.AdjustOnHand(delta);
            created.Add($"inventory top-up for SKU '{VariantSku}' (+{delta} on hand, available now {inventory.Available})");
        }
        else
        {
            skipped.Add($"inventory for SKU '{VariantSku}'");
        }

        var zone = await _db.DeliveryZones
            .FirstOrDefaultAsync(z => z.Name == DeliveryZoneName, cancellationToken);
        if (zone is null)
        {
            zone = DeliveryZone.Create(DeliveryZoneName, DeliveryZoneFee, true);
            _db.DeliveryZones.Add(zone);
            created.Add($"delivery zone '{DeliveryZoneName}'");
        }
        else
        {
            skipped.Add($"delivery zone '{DeliveryZoneName}'");
        }

        if (!await _db.OrderingSettings.AnyAsync(cancellationToken))
        {
            _db.OrderingSettings.Add(OrderingSettings.CreateDefault(0m));
            created.Add("ordering settings (minimum 0)");
        }
        else
        {
            skipped.Add("ordering settings");
        }

        if (created.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);

        if (created.Count > 0)
            _logger.LogInformation(
                "Local dev catalog fixtures created: {Created}",
                string.Join("; ", created));
        if (skipped.Count > 0)
            _logger.LogInformation(
                "Local dev catalog fixtures skipped (already present): {Skipped}",
                string.Join("; ", skipped));
    }
}
