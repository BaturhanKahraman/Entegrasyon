namespace Entegrasyon.Entity.Dtos.Category;

/// <summary>
/// Kategori eşleştirmesi kaldırılmadan önce kullanıcıya gösterilecek etki özeti.
/// </summary>
public class CategoryUnmapPreviewDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int MarketPlaceId { get; set; }

    /// <summary>Bu eşleştirmeyi kullanan aktif ürün sayısı.</summary>
    public int AffectedProductCount { get; set; }

    /// <summary>Başka eşleşmiş kategoride kullanılmadığı için silinecek özellik eşleşmeleri.</summary>
    public List<OrphanedMatchDto> OrphanedAttributeMatches { get; set; } = [];

    /// <summary>Başka eşleşmiş kategoride kullanılmadığı için silinecek özellik değeri eşleşmeleri.</summary>
    public List<OrphanedMatchDto> OrphanedValueMatches { get; set; } = [];
}

public class OrphanedMatchDto
{
    public int ApplicationId { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public int MarketPlaceMatchId { get; set; }
}
