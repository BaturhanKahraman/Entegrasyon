namespace Entegrasyon.Entity.Dtos.Brand;

/// <summary>
/// Brand mapping'lerinin özet istatistiklerini gösterir.
/// </summary>
public record BrandMappingSummaryDto
{
    public int TotalBrands { get; set; }
    public int MappedBrands { get; set; }
    public int UnmappedBrands { get; set; }
}
