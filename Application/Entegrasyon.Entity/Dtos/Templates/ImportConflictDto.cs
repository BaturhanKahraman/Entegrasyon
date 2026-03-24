namespace Entegrasyon.Entity.Dtos.Templates;

/// <summary>
/// Import sırasında tespit edilen çakışma bilgisi.
/// </summary>
public record ImportConflictDto
{
    public string EntityType { get; init; } = null!;
    public int TemplateEntityId { get; init; }
    public string TemplateName { get; init; } = null!;
    public int ExistingEntityId { get; init; }
    public string ExistingEntityName { get; init; } = null!;
    public ConflictType ConflictType { get; init; }
}

public enum ConflictType
{
    NameMatch,
    ExternalIdMatch,
    Both
}
