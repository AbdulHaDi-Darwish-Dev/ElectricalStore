using ElectricalStore.Domain.Catalog.Categories;
using Xunit;

namespace ElectricalStore.Domain.Tests.Catalog.Categories;

public sealed class CategoryTests
{
    [Fact]
    public void Create_ValidCategory_Succeeds()
    {
        var category = Category.Create("  Cables  ", "  Wire products  ", isActive: true);

        Assert.NotEqual(Guid.Empty, category.Id);
        Assert.Equal("Cables", category.Name);
        Assert.Equal("CABLES", category.NormalizedName);
        Assert.Equal("Wire products", category.Description);
        Assert.True(category.IsActive);
    }

    [Fact]
    public void Create_BlankName_Throws()
    {
        Assert.Throws<ArgumentException>(() => Category.Create("   ", null, true));
    }

    [Fact]
    public void Create_OptionalDescription_BecomesNullWhenBlank()
    {
        var category = Category.Create("Tools", "   ", isActive: false);

        Assert.Null(category.Description);
        Assert.False(category.IsActive);
    }

    [Fact]
    public void Activate_And_Deactivate_AreIdempotent()
    {
        var category = Category.Create("Switches", null, isActive: false);

        category.Activate();
        Assert.True(category.IsActive);
        category.Activate();
        Assert.True(category.IsActive);

        category.Deactivate();
        Assert.False(category.IsActive);
        category.Deactivate();
        Assert.False(category.IsActive);
    }

    [Fact]
    public void Update_ChangesNameAndDescription()
    {
        var category = Category.Create("Old", "Desc", true);

        category.Update("  New Name ", "  Updated ");

        Assert.Equal("New Name", category.Name);
        Assert.Equal("NEW NAME", category.NormalizedName);
        Assert.Equal("Updated", category.Description);
    }

    [Fact]
    public void SetImage_And_ClearImage_Work()
    {
        var category = Category.Create("Lights", null, true);
        Assert.False(category.HasImage);

        category.SetImage("folder/id", "https://cdn.test/x.jpg");
        Assert.True(category.HasImage);
        Assert.Equal("folder/id", category.ImageStorageKey);
        Assert.Equal("https://cdn.test/x.jpg", category.ImageUrl);

        var previous = category.ClearImage();
        Assert.Equal("folder/id", previous);
        Assert.False(category.HasImage);
        Assert.Null(category.ImageStorageKey);
        Assert.Null(category.ImageUrl);
    }
}
