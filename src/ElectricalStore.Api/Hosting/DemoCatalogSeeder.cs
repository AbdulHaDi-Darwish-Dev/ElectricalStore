using ElectricalStore.Domain.Catalog.Categories;
using ElectricalStore.Domain.Catalog.Products;
using ElectricalStore.Domain.Inventory;
using ElectricalStore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ElectricalStore.Api.Hosting;

/// <summary>
/// Idempotent Development demo catalog seeder (Arabic-rich storefront data).
/// Creates missing DEMO-* records only; never deletes arbitrary catalog rows.
/// </summary>
public sealed class DemoCatalogSeeder
{
    private readonly AppDbContext _db;
    private readonly DemoCatalogOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly IWebHostEnvironment _webHost;
    private readonly ILogger<DemoCatalogSeeder> _logger;

    public DemoCatalogSeeder(
        AppDbContext db,
        IOptions<DemoCatalogOptions> options,
        IHostEnvironment environment,
        IWebHostEnvironment webHost,
        ILogger<DemoCatalogSeeder> logger)
    {
        _db = db;
        _options = options.Value;
        _environment = environment;
        _webHost = webHost;
        _logger = logger;
    }

    public async Task<DemoCatalogSeedResult> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment())
        {
            _logger.LogWarning("DemoCatalog seeding refused outside Development.");
            return DemoCatalogSeedResult.NotRun("Not Development.");
        }

        if (!_options.Enabled)
            return DemoCatalogSeedResult.NotRun("DemoCatalog:Enabled=false.");

        var created = new List<string>();
        var skipped = new List<string>();
        var categoryIds = new Dictionary<string, Guid>(StringComparer.Ordinal);

        foreach (var categoryDef in DemoCatalogDefinitions.Categories)
        {
            var existing = await _db.Categories
                .FirstOrDefaultAsync(
                    c => c.NormalizedName == Category.NormalizeNameKey(categoryDef.Name),
                    cancellationToken);

            if (existing is null)
            {
                existing = Category.Create(categoryDef.Name, categoryDef.Description, isActive: true);
                TrySetCategoryImage(existing, categoryDef.ImageFile, created);
                _db.Categories.Add(existing);
                created.Add($"category '{categoryDef.Name}'");
            }
            else
            {
                skipped.Add($"category '{categoryDef.Name}'");
                if (!existing.HasImage)
                    TrySetCategoryImage(existing, categoryDef.ImageFile, created);
            }

            categoryIds[categoryDef.Key] = existing.Id;
        }

        // Persist categories first so product FKs resolve cleanly on first run.
        if (created.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);

        var existingDemoSkus = await _db.ProductVariants
            .AsNoTracking()
            .Where(v => v.Sku.StartsWith(DemoCatalogDefinitions.SkuPrefix))
            .Select(v => new { v.Sku, v.ProductId })
            .ToListAsync(cancellationToken);

        var skuToProductId = existingDemoSkus
            .GroupBy(x => x.Sku, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().ProductId, StringComparer.OrdinalIgnoreCase);

        foreach (var productDef in DemoCatalogDefinitions.Products)
        {
            if (!categoryIds.TryGetValue(productDef.CategoryKey, out var categoryId))
            {
                _logger.LogError(
                    "DemoCatalog product '{Name}' references unknown category key '{Key}'.",
                    productDef.Name,
                    productDef.CategoryKey);
                continue;
            }

            var primarySku = productDef.Variants[0].Sku;
            Product? product = null;

            if (skuToProductId.TryGetValue(primarySku, out var productId))
            {
                product = await _db.Products
                    .Include(p => p.Variants)
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
            }

            if (product is null)
            {
                // Avoid colliding with a same-named non-demo product.
                var nameTaken = await _db.Products
                    .AsNoTracking()
                    .AnyAsync(
                        p => p.Name == productDef.Name.Trim(),
                        cancellationToken);
                if (nameTaken)
                {
                    skipped.Add($"product name collision '{productDef.Name}'");
                    continue;
                }

                var seeds = productDef.Variants
                    .Select(v => new ProductVariantSeed(
                        v.Name,
                        v.Sku,
                        v.Price,
                        v.SellingUnit,
                        v.QuantityIncrement,
                        v.IsActive))
                    .ToList();

                product = Product.Create(
                    productDef.Name,
                    productDef.Description,
                    categoryId,
                    productDef.IsActive,
                    seeds);

                TryAddProductImage(product, productDef.ImageFile, created);
                _db.Products.Add(product);
                created.Add($"product '{productDef.Name}'");

                foreach (var variantDef in productDef.Variants)
                    skuToProductId[variantDef.Sku] = product.Id;
            }
            else
            {
                skipped.Add($"product '{productDef.Name}'");

                foreach (var variantDef in productDef.Variants)
                {
                    var hasVariant = product.Variants.Any(v =>
                        string.Equals(v.Sku, variantDef.Sku, StringComparison.OrdinalIgnoreCase));
                    if (hasVariant)
                    {
                        skipped.Add($"variant '{variantDef.Sku}'");
                        continue;
                    }

                    var skuExistsElsewhere = await _db.ProductVariants
                        .AsNoTracking()
                        .AnyAsync(
                            v => v.NormalizedSku == ProductVariant.NormalizeSkuKey(variantDef.Sku),
                            cancellationToken);
                    if (skuExistsElsewhere)
                    {
                        skipped.Add($"variant sku busy '{variantDef.Sku}'");
                        continue;
                    }

                    product.AddVariant(new ProductVariantSeed(
                        variantDef.Name,
                        variantDef.Sku,
                        variantDef.Price,
                        variantDef.SellingUnit,
                        variantDef.QuantityIncrement,
                        variantDef.IsActive));
                    created.Add($"variant '{variantDef.Sku}'");
                }

                if (!product.HasImages)
                    TryAddProductImage(product, productDef.ImageFile, created);
            }

            // Inventory for each variant belonging to this definition.
            foreach (var variantDef in productDef.Variants)
            {
                var variant = product.Variants.FirstOrDefault(v =>
                    string.Equals(v.Sku, variantDef.Sku, StringComparison.OrdinalIgnoreCase));
                if (variant is null)
                    continue;

                var inventory = await _db.InventoryItems
                    .FirstOrDefaultAsync(i => i.ProductVariantId == variant.Id, cancellationToken);
                if (inventory is null)
                {
                    inventory = InventoryItem.CreateZero(variant.Id);
                    if (variantDef.OnHand > 0m)
                        inventory.AdjustOnHand(variantDef.OnHand);
                    _db.InventoryItems.Add(inventory);
                    created.Add($"inventory '{variantDef.Sku}' onHand={variantDef.OnHand}");
                }
                else
                {
                    skipped.Add($"inventory '{variantDef.Sku}'");
                }
            }
        }

        if (_db.ChangeTracker.HasChanges())
            await _db.SaveChangesAsync(cancellationToken);

        var stats = await CollectStatsAsync(cancellationToken);

        _logger.LogInformation(
            "DemoCatalog seed finished. Created={CreatedCount}, Skipped={SkippedCount}, Categories={Categories}, Products={Products}, Variants={Variants}, Images={Images}.",
            created.Count,
            skipped.Count,
            stats.Categories,
            stats.Products,
            stats.Variants,
            stats.ProductImages);

        return new DemoCatalogSeedResult(true, null, created, skipped, stats);
    }

    private void TrySetCategoryImage(Category category, string relativeFile, List<string> created)
    {
        var resolved = ResolvePublicAsset(relativeFile);
        if (resolved is null)
            return;

        category.SetImage(resolved.StorageKey, resolved.Url);
        created.Add($"category image '{relativeFile}'");
    }

    private void TryAddProductImage(Product product, string relativeFile, List<string> created)
    {
        var resolved = ResolvePublicAsset(relativeFile);
        if (resolved is null)
            return;

        product.AddImage(resolved.StorageKey, resolved.Url);
        created.Add($"product image '{relativeFile}'");
    }

    private DemoAssetRef? ResolvePublicAsset(string relativeFile)
    {
        var relative = relativeFile.Replace('\\', '/').TrimStart('/');
        var physical = Path.Combine(_webHost.WebRootPath ?? string.Empty, "demo-catalog", relative);
        if (!File.Exists(physical))
            return null;

        var baseUrl = (_options.PublicBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = "http://localhost:5180";

        var url = $"{baseUrl}/demo-catalog/{relative}";
        var storageKey = DemoCatalogDefinitions.StorageKeyPrefix + relative;
        return new DemoAssetRef(storageKey, url);
    }

    private async Task<DemoCatalogStats> CollectStatsAsync(CancellationToken cancellationToken)
    {
        var categoryNames = DemoCatalogDefinitions.Categories.Select(c => c.Name).ToList();
        var categories = await _db.Categories.CountAsync(
            c => categoryNames.Contains(c.Name),
            cancellationToken);

        var variants = await _db.ProductVariants
            .Where(v => v.Sku.StartsWith(DemoCatalogDefinitions.SkuPrefix))
            .Select(v => new { v.Id, v.ProductId })
            .ToListAsync(cancellationToken);

        var productIds = variants.Select(v => v.ProductId).Distinct().ToList();
        var products = productIds.Count;
        var images = await _db.ProductImages.CountAsync(
            i => productIds.Contains(i.ProductId),
            cancellationToken);
        var variantIds = variants.Select(v => v.Id).ToList();
        var stocked = variantIds.Count == 0
            ? 0
            : await _db.InventoryItems.CountAsync(
                i => variantIds.Contains(i.ProductVariantId) && i.OnHand > 0,
                cancellationToken);

        return new DemoCatalogStats(categories, products, variants.Count, images, stocked);
    }

    private sealed record DemoAssetRef(string StorageKey, string Url);
}

public sealed record DemoCatalogStats(
    int Categories,
    int Products,
    int Variants,
    int ProductImages,
    int StockedVariants);

public sealed record DemoCatalogSeedResult(
    bool Ran,
    string? SkipReason,
    IReadOnlyList<string> Created,
    IReadOnlyList<string> Skipped,
    DemoCatalogStats? Stats)
{
    public static DemoCatalogSeedResult NotRun(string reason) =>
        new(false, reason, Array.Empty<string>(), Array.Empty<string>(), null);
}
