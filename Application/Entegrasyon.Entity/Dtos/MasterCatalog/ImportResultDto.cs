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
    int ValuesSkipped,
    int BrandsImported = 0,
    int BrandsSkipped = 0)
{
    public int TotalImported => CategoriesImported + AttributesImported + ValuesImported + MappingsImported + BrandsImported;
    public int TotalSkipped => CategoriesSkipped + AttributesSkipped + ValuesSkipped + BrandsSkipped;
}
