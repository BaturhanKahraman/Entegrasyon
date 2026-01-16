namespace Entegrasyon.Blazor.Models;

/// <summary>
/// Represents a permission for display and selection in role management
/// </summary>
public class PermissionModel
{
    public string Name { get; set; }
    public string Category { get; set; }
    public string DisplayName { get; set; }

    public PermissionModel(string name, string displayName)
    {
        Name = name;
        DisplayName = displayName;

        // Extract category from permission name (e.g., "Permissions.Users.View" -> "Users")
        var parts = name.Split('.');
        Category = parts.Length > 1 ? parts[1] : "Other";
    }
}
