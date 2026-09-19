namespace ElectricalStore.Application.Authorization;

/// <summary>
/// Application-owned permission catalog (not Permixa IAM permissions).
/// </summary>
public static class AppPermissions
{
    public static class Categories
    {
        public const string Manage = "Categories.Manage";
    }

    public static class Products
    {
        public const string Manage = "Products.Manage";
    }

    public static class Inventory
    {
        public const string Read = "Inventory.Read";
        public const string Adjust = "Inventory.Adjust";
    }

    public static class Shipping
    {
        public const string Manage = "Shipping.Manage";
    }

    public static class Orders
    {
        public const string Read = "Orders.Read";
        public const string Manage = "Orders.Manage";
    }

    public static class Settings
    {
        public const string Manage = "Settings.Manage";
    }

    public static IReadOnlyList<string> All { get; } =
    [
        Categories.Manage,
        Products.Manage,
        Inventory.Read,
        Inventory.Adjust,
        Shipping.Manage,
        Orders.Read,
        Orders.Manage,
        Settings.Manage
    ];
}
