namespace Entegrasyon.Blazor.ViewModels;

/// <summary>
/// View model for category tree representation in import flows.
/// Used to display hierarchical category structures from external sources (marketplaces, APIs).
/// </summary>
public sealed class CategoryTreeNode : IEquatable<CategoryTreeNode>
{
    /// <summary>
    /// External category ID from the source system (e.g., Trendyol category ID).
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the category.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// External ID of the parent category, if applicable.
    /// </summary>
    public string? ParentExternalId { get; set; }

    /// <summary>
    /// Child categories in the hierarchy.
    /// </summary>
    public HashSet<CategoryTreeNode> Children { get; set; } = new();

    /// <summary>
    /// Indicates whether this node has child categories.
    /// </summary>
    public bool HasChildren => Children.Count > 0;

    /// <summary>
    /// Indicates whether this node can be expanded (may have children not yet loaded).
    /// Used for lazy-loading: true means "try to load children on expand".
    /// </summary>
    public bool CanExpand { get; set; }

    /// <summary>
    /// UI state: whether this node is expanded in the tree view.
    /// </summary>
    public bool IsExpanded { get; set; }

    public bool Equals(CategoryTreeNode? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return ExternalId == other.ExternalId;
    }

    public override bool Equals(object? obj) => Equals(obj as CategoryTreeNode);

    public override int GetHashCode() => ExternalId.GetHashCode();

    public static bool operator ==(CategoryTreeNode? left, CategoryTreeNode? right) =>
        Equals(left, right);

    public static bool operator !=(CategoryTreeNode? left, CategoryTreeNode? right) =>
        !Equals(left, right);
}
