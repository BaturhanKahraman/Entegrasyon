namespace Entegrasyon.Entity.Dtos.Reports;

public sealed record SalesReportFilterDto(
    DateOnly StartDate,
    DateOnly EndDate);

public sealed record SalesReportDto(
    SalesReportSummaryDto Summary,
    List<DailySalesReportDto> DailySales,
    List<TopSellingProductDto> TopProducts);

public sealed record SalesReportSummaryDto(
    int TotalSales,
    decimal TotalRevenue,
    decimal AverageOrderValue,
    int TotalItemsSold);

public sealed record DailySalesReportDto(
    DateOnly Date,
    int SaleCount,
    decimal Revenue);

public sealed record TopSellingProductDto(
    string ProductTitle,
    string VariantTitle,
    int QuantitySold,
    decimal Revenue);
