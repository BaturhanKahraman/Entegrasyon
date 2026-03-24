namespace Entegrasyon.Entity.Templates;

/// <summary>
/// Template entity'ler için base class.
/// BaseEntity'den türemez çünkü soft-delete ve global query filter gereksizdir — admin referans verisi.
/// </summary>
public abstract class TemplateBaseEntity
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
