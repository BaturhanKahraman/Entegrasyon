namespace Entegrasyon.Entity.Dtos.Product.Marketplace;

// Yazma DTO'su
public sealed class SaveMarketplaceOverridesDto
{
    public Guid ProductId { get; set; }
    public int MarketPlaceId { get; set; }
    public string? TitleOverride { get; set; }
    public string? DescriptionOverride { get; set; }
    public List<VariantPriceOverrideDto> VariantOverrides { get; set; } = [];
}

public sealed class VariantPriceOverrideDto
{
    public Guid ProductVariantId { get; set; }
    public decimal? ListPriceOverride { get; set; }
    public decimal? SalePriceOverride { get; set; }
}

// Okuma DTO'su — UI'da mevcut override durumunu göstermek için
public sealed class MarketplaceOverrideDetailDto
{
    public int MarketPlaceId { get; set; }
    public string MarketPlaceName { get; set; } = "";
    public string? TitleOverride { get; set; }
    public string? DescriptionOverride { get; set; }
    public List<VariantPriceOverrideDetailDto> VariantOverrides { get; set; } = [];
}

public sealed class VariantPriceOverrideDetailDto
{
    public Guid ProductVariantId { get; set; }
    public string VariantLabel { get; set; } = "";
    public string Barcode { get; set; } = "";
    public decimal OriginalListPrice { get; set; }
    public decimal OriginalSalePrice { get; set; }
    public decimal? ListPriceOverride { get; set; }
    public decimal? SalePriceOverride { get; set; }
}
