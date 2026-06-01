namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontAddress : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int CustomerId { get; set; }
    public string Label { get; set; } = null!;        // "Ev", "İş" vb.
    public string FullName { get; set; } = null!;      // teslim alacak kişi
    public string? Phone { get; set; }
    public string? City { get; set; }                  // İl
    public string? District { get; set; }              // İlçe
    public string? Neighborhood { get; set; }          // Mahalle
    public string? PostalCode { get; set; }
    public string AddressLine { get; set; } = null!;   // açık adres
    public bool IsDefault { get; set; }
}
