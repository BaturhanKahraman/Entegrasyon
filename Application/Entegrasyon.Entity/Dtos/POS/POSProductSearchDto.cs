namespace Entegrasyon.Entity.Dtos.POS;

public sealed class POSProductSearchResultDto
{
    public List<POSProductSearchItemDto> Products { get; set; } = [];
    public POSProductSearchVariantDto? ExactBarcodeMatch { get; set; }
}

public sealed class POSProductSearchItemDto
{
    public Guid ProductId { get; set; }
    public string Title { get; set; } = "";
    public string StockCode { get; set; } = "";
    public string BrandName { get; set; } = "";
    public string? FeaturedImageUrl { get; set; }
    public List<POSProductSearchVariantDto> Variants { get; set; } = [];
}

public sealed class POSProductSearchVariantDto
{
    public Guid ProductId { get; set; }
    public Guid VariantId { get; set; }
    public string ProductTitle { get; set; } = "";
    public string Barcode { get; set; } = "";
    public string? ImageUrl { get; set; }
    public decimal SalePrice { get; set; }
    public decimal ListPrice { get; set; }
    public decimal VatRate { get; set; }
    public int CurrentStock { get; set; }
    public string DisplayName { get; set; } = "";
}
