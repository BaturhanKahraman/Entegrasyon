namespace Entegrasyon.Entity.BulkOperations;

/// <summary>
/// Kullanicilarin import sirasinda kolon eslestirme profillerini saklar.
/// MappingsJson: {"SystemField": "ExcelColumnName"} formati.
/// </summary>
public sealed class ImportColumnProfile : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public BulkOperationType ImportType { get; set; }
    public string MappingsJson { get; set; } = "{}";
}
