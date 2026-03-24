namespace Entegrasyon.Entity.Dtos.Reports;

public sealed record ProfitLossReportFilterDto(
    DateOnly StartDate,
    DateOnly EndDate,
    int? MarketPlaceId = null);

public sealed record ProfitLossReportDto(
    decimal Revenue,
    decimal CostOfGoods,
    decimal MarketplaceCommission,
    decimal CargoExpense,
    decimal TaxAmount,
    decimal NetProfit,
    decimal ProfitMargin,
    List<ProfitLossByMarketplaceDto> ByMarketplace);

public sealed record ProfitLossByMarketplaceDto(
    int MarketPlaceId,
    string MarketPlaceName,
    decimal Revenue,
    decimal Commission,
    decimal NetProfit);
