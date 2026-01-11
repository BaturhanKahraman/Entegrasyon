using System.Collections.ObjectModel;

namespace Shared.Extensions.Constants;

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

    public static List<string> GetAllPermissions()
    {
        var permissions = new List<string>();
        // Reflection could be used here to dynamically get all string constants
        permissions.AddRange(new[] { Users.View, Users.Create, Users.Edit, Users.Delete });
        permissions.AddRange(new[] { Roles.View, Roles.Create, Roles.Edit, Roles.Delete });
        permissions.AddRange(new[] { Products.View, Products.Create, Products.Edit, Products.Delete });
        return permissions;
    }
}
