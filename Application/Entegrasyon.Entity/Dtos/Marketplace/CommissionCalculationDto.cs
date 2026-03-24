namespace Entegrasyon.Entity.Dtos.Marketplace;

/// <summary>
/// Komisyon hesaplama sonucu
/// </summary>
public sealed record CommissionCalculationResult
{
    public decimal SalePrice { get; init; }
    public decimal CommissionAmount { get; init; }
    public decimal ServiceFeeAmount { get; init; }
    public decimal TransactionFee { get; init; }
    public decimal TotalDeductions { get; init; }
    public decimal NetRevenue { get; init; }
    public decimal CostPrice { get; init; }
    public decimal NetProfit { get; init; }
    public decimal ProfitMarginPercent { get; init; }
}

/// <summary>
/// Komisyon oranı okuma DTO'su
/// </summary>
public sealed class MarketplaceCommissionRateDto
{
    public int Id { get; set; }
    public int MarketPlaceId { get; set; }
    public string MarketPlaceName { get; set; } = "";
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public decimal CommissionPercent { get; set; }
    public decimal? ServiceFeePercent { get; set; }
    public decimal? TransactionFeeFixed { get; set; }
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
}

/// <summary>
/// Komisyon oranı kaydetme DTO'su
/// </summary>
public sealed class SaveCommissionRateDto
{
    public int? Id { get; set; }
    public int MarketPlaceId { get; set; }
    public int? CategoryId { get; set; }
    public decimal CommissionPercent { get; set; }
    public decimal? ServiceFeePercent { get; set; }
    public decimal? TransactionFeeFixed { get; set; }
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
}
