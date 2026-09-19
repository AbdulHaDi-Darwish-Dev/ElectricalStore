using ElectricalStore.Domain.Catalog.Products;
using Xunit;

namespace ElectricalStore.Domain.Tests.Catalog.Products;

public sealed class ProductTests
{
    private static ProductVariantSeed StandardVariant(
        string name = "Standard",
        string sku = "SKU-001",
        decimal price = 100m,
        SellingUnit unit = SellingUnit.Piece,
        decimal increment = 1m,
        bool isActive = true) =>
        new(name, sku, price, unit, increment, isActive);

    [Fact]
    public void Create_WithOneVariant_Succeeds()
    {
        var product = Product.Create("  Cable  ", "  Desc  ", Guid.NewGuid(), true, [StandardVariant()]);

        Assert.Equal("Cable", product.Name);
        Assert.Equal("Desc", product.Description);
        Assert.True(product.IsActive);
        Assert.Single(product.Variants);
        Assert.Equal("SKU-001", product.Variants.First().Sku);
        Assert.Equal("SKU-001", product.Variants.First().NormalizedSku);
    }

    [Fact]
    public void Create_WithMultipleVariants_Succeeds()
    {
        var product = Product.Create("Wire", null, Guid.NewGuid(), true,
        [
            StandardVariant("1mm", "W-1", 10m),
            StandardVariant("2mm", "W-2", 20m, SellingUnit.Meter, 0.5m)
        ]);

        Assert.Equal(2, product.Variants.Count);
        Assert.Contains(product.Variants, v => v.SellingUnit == SellingUnit.Meter && v.QuantityIncrement == 0.5m);
    }

    [Fact]
    public void Create_WithoutVariants_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Product.Create("X", null, Guid.NewGuid(), true, Array.Empty<ProductVariantSeed>()));
    }

    [Fact]
    public void Create_InvalidPrice_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Product.Create("X", null, Guid.NewGuid(), true, [StandardVariant(price: 0m)]));
    }

    [Fact]
    public void Create_PieceIncrementNotOne_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Product.Create("X", null, Guid.NewGuid(), true,
                [StandardVariant(unit: SellingUnit.Piece, increment: 2m)]));
    }

    [Fact]
    public void Create_MeterDecimalIncrement_Accepted()
    {
        var product = Product.Create("X", null, Guid.NewGuid(), true,
            [StandardVariant(unit: SellingUnit.Meter, increment: 0.5m)]);

        Assert.Equal(0.5m, product.Variants.First().QuantityIncrement);
    }

    [Fact]
    public void Create_DuplicateVariantNames_CaseInsensitive_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Product.Create("X", null, Guid.NewGuid(), true,
            [
                StandardVariant("Std", "A-1"),
                StandardVariant("std", "A-2")
            ]));
    }

    [Fact]
    public void Create_DuplicateSkus_CaseInsensitive_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Product.Create("X", null, Guid.NewGuid(), true,
            [
                StandardVariant("A", "sku-1"),
                StandardVariant("B", "SKU-1")
            ]));
    }

    [Fact]
    public void Create_EmptyCategory_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Product.Create("X", null, Guid.Empty, true, [StandardVariant()]));
    }

    [Fact]
    public void Activate_Deactivate_ProductAndVariant()
    {
        var product = Product.Create("X", null, Guid.NewGuid(), false, [StandardVariant(isActive: false)]);
        product.Activate();
        Assert.True(product.IsActive);

        var variantId = product.Variants.First().Id;
        product.ActivateVariant(variantId);
        Assert.True(product.Variants.First().IsActive);

        product.DeactivateVariant(variantId);
        Assert.False(product.Variants.First().IsActive);

        product.Deactivate();
        Assert.False(product.IsActive);
    }

    [Fact]
    public void AddVariant_RejectsDuplicateNameInsideAggregate()
    {
        var product = Product.Create("X", null, Guid.NewGuid(), true, [StandardVariant("Std", "S1")]);
        Assert.Throws<ArgumentException>(() =>
            product.AddVariant(StandardVariant("std", "S2")));
    }

    [Fact]
    public void Images_FirstIsPrimary_FifthRejected_SetPrimary_DeletePromotes()
    {
        var product = Product.Create("X", null, Guid.NewGuid(), true, [StandardVariant()]);

        var first = product.AddImage("p/1", "https://cdn.test/1.jpg");
        Assert.True(first.IsPrimary);
        Assert.Equal(0, first.SortOrder);

        product.AddImage("p/2", "https://cdn.test/2.jpg");
        product.AddImage("p/3", "https://cdn.test/3.jpg");
        var fourth = product.AddImage("p/4", "https://cdn.test/4.jpg");
        Assert.False(fourth.IsPrimary);
        Assert.Throws<InvalidOperationException>(() => product.AddImage("p/5", "https://cdn.test/5.jpg"));

        product.SetPrimaryImage(fourth.Id);
        Assert.True(product.Images.Single(i => i.Id == fourth.Id).IsPrimary);
        Assert.Equal(1, product.Images.Count(i => i.IsPrimary));

        product.RemoveImage(fourth.Id);
        Assert.Equal(3, product.Images.Count);
        Assert.Single(product.Images, i => i.IsPrimary);
        Assert.Equal(0, product.Images.OrderBy(i => i.SortOrder).First().SortOrder);

        foreach (var image in product.Images.ToList())
            product.RemoveImage(image.Id);

        Assert.Empty(product.Images);
        Assert.False(product.HasImages);
    }

    [Fact]
    public void ReorderImages_UpdatesSortOrder()
    {
        var product = Product.Create("X", null, Guid.NewGuid(), true, [StandardVariant()]);
        var a = product.AddImage("p/a", "https://cdn.test/a.jpg");
        var b = product.AddImage("p/b", "https://cdn.test/b.jpg");
        var c = product.AddImage("p/c", "https://cdn.test/c.jpg");

        product.ReorderImages([c.Id, a.Id, b.Id]);

        Assert.Equal(0, product.Images.Single(i => i.Id == c.Id).SortOrder);
        Assert.Equal(1, product.Images.Single(i => i.Id == a.Id).SortOrder);
        Assert.Equal(2, product.Images.Single(i => i.Id == b.Id).SortOrder);
    }
}
