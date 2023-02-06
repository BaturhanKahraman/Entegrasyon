using Shared.Entity;

namespace Entegrasyon.Entity.Barcode;

public sealed class TempBarcode:BaseEntity
{
    public int Id { get; set; }
    public string Barcode { get; set; }
    public bool IsAdded { get; set; }
    public bool IsAddable { get; set; }
    public DateTimeOffset ValidUntil { get; set; }
}