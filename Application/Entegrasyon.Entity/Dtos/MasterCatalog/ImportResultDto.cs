namespace Entegrasyon.Entity.Dtos.MasterCatalog;

/// <summary>
/// Master catalog tenant import işleminin sonuç özeti.
/// </summary>
public record ImportResultDto(
    int CategoriesImported,
    int AttributesImported,
    int ValuesImported,
    int MappingsImported,
    int CategoriesSkipped,
    int AttributesSkipped,
    int ValuesSkipped)
{
    public int TotalImported => CategoriesImported + AttributesImported + ValuesImported + MappingsImported;
    public int TotalSkipped => CategoriesSkipped + AttributesSkipped + ValuesSkipped;
}
