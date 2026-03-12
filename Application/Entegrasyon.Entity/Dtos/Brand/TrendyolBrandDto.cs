namespace Entegrasyon.Entity.Dtos.Brand;

/// <summary>
/// Trendyol platformundaki brand bilgilerini temsil eder.
/// </summary>
public record TrendyolBrandDto
{
    public int Id { get; set; }
    public string Name { get; set; }
}
