using System.Collections.ObjectModel;

namespace Entegrasyon.ApplicationBootstrap.Security;

public static class AppPermissions
{
    public static class Users
    {
        public const string View = "Permissions.Users.View";
        public const string Create = "Permissions.Users.Create";
        public const string Edit = "Permissions.Users.Edit";
        public const string Delete = "Permissions.Users.Delete";
    }

    public static class Roles
    {
        public const string View = "Permissions.Roles.View";
        public const string Create = "Permissions.Roles.Create";
        public const string Edit = "Permissions.Roles.Edit";
        public const string Delete = "Permissions.Roles.Delete";
    }

    public static class Products
    {
        public const string View = "Permissions.Products.View";
        public const string Create = "Permissions.Products.Create";
        public const string Edit = "Permissions.Products.Edit";
        public const string Delete = "Permissions.Products.Delete";
    }

    public static class Customers
    {
        public const string View = "Permissions.Customers.View";
        public const string Create = "Permissions.Customers.Create";
        public const string Edit = "Permissions.Customers.Edit";
        public const string Delete = "Permissions.Customers.Delete";
    }

    public static class BranchOffices
    {
        public const string View = "Permissions.BranchOffices.View";
        public const string Create = "Permissions.BranchOffices.Create";
        public const string Edit = "Permissions.BranchOffices.Edit";
        public const string Delete = "Permissions.BranchOffices.Delete";
    }

    public static class Categories
    {
        public const string View = "Permissions.Categories.View";
        public const string Create = "Permissions.Categories.Create";
        public const string Edit = "Permissions.Categories.Edit";
        public const string Delete = "Permissions.Categories.Delete";
    }

    public static class Orders
    {
        public const string View = "Permissions.Orders.View";
        public const string Create = "Permissions.Orders.Create";
        public const string Edit = "Permissions.Orders.Edit";
        public const string Delete = "Permissions.Orders.Delete";
    }

    public static class Sales
    {
        public const string View = "Permissions.Sales.View";
        public const string Create = "Permissions.Sales.Create";
        public const string Edit = "Permissions.Sales.Edit";
        public const string Delete = "Permissions.Sales.Delete";
    }

    public static class Reports
    {
        public const string View = "Permissions.Reports.View";
        public const string Create = "Permissions.Reports.Create";
        public const string Edit = "Permissions.Reports.Edit";
        public const string Delete = "Permissions.Reports.Delete";
    }

    public static class Cargo
    {
        public const string View = "Permissions.Cargo.View";
        public const string Create = "Permissions.Cargo.Create";
        public const string Edit = "Permissions.Cargo.Edit";
        public const string Delete = "Permissions.Cargo.Delete";
    }

    public static class Integrations
    {
        public const string View = "Permissions.Integrations.View";
        public const string Create = "Permissions.Integrations.Create";
        public const string Edit = "Permissions.Integrations.Edit";
        public const string Delete = "Permissions.Integrations.Delete";
    }

    public static class Logs
    {
        public const string View = "Permissions.Logs.View";
    }

    public static class Settings
    {
        public const string View = "Permissions.Settings.View";
        public const string Edit = "Permissions.Settings.Edit";
    }

    public static class Brands
    {
        public const string View = "Permissions.Brands.View";
        public const string Create = "Permissions.Brands.Create";
        public const string Edit = "Permissions.Brands.Edit";
        public const string Delete = "Permissions.Brands.Delete";
    }

    public static class Marketplace
    {
        public const string View = "Permissions.Marketplace.View";
        public const string Create = "Permissions.Marketplace.Create";
        public const string Edit = "Permissions.Marketplace.Edit";
        public const string Delete = "Permissions.Marketplace.Delete";
    }

    public static class Notifications
    {
        public const string View = "Permissions.Notifications.View";
    }

    public static List<string> GetAllPermissions()
    {
        var permissions = new List<string>();
        permissions.AddRange([Users.View, Users.Create, Users.Edit, Users.Delete]);
        permissions.AddRange([Roles.View, Roles.Create, Roles.Edit, Roles.Delete]);
        permissions.AddRange([Products.View, Products.Create, Products.Edit, Products.Delete]);
        permissions.AddRange([Customers.View, Customers.Create, Customers.Edit, Customers.Delete]);
        permissions.AddRange([BranchOffices.View, BranchOffices.Create, BranchOffices.Edit, BranchOffices.Delete]);
        permissions.AddRange([Categories.View, Categories.Create, Categories.Edit, Categories.Delete]);
        permissions.AddRange([Orders.View, Orders.Create, Orders.Edit, Orders.Delete]);
        permissions.AddRange([Sales.View, Sales.Create, Sales.Edit, Sales.Delete]);
        permissions.AddRange([Reports.View, Reports.Create, Reports.Edit, Reports.Delete]);
        permissions.AddRange([Cargo.View, Cargo.Create, Cargo.Edit, Cargo.Delete]);
        permissions.AddRange([Integrations.View, Integrations.Create, Integrations.Edit, Integrations.Delete]);
        permissions.AddRange([Brands.View, Brands.Create, Brands.Edit, Brands.Delete]);
        permissions.AddRange([Logs.View]);
        permissions.AddRange([Settings.View, Settings.Edit]);
        permissions.AddRange([Marketplace.View, Marketplace.Create, Marketplace.Edit, Marketplace.Delete]);
        permissions.AddRange([Notifications.View]);
        return permissions;
    }
}
