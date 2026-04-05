namespace Entegrasyon.Entity.Products;

public record ProductChangeEntry(
    DateTimeOffset ChangedAt,
    string ChangedBy,
    Dictionary<string, ProductFieldChange> Changes
);

public record ProductFieldChange(string? OldValue, string? NewValue);
