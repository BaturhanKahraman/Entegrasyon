namespace Entegrasyon.Entity.Dtos.Reports;

public sealed record MarketplaceReportDto(
    MarketplaceReportSummaryDto Summary,
    List<MarketplaceProductStatusDto> FailedProducts);

public sealed record MarketplaceReportSummaryDto(
    int TotalProducts,
    int PublishedCount,
    int PendingCount,
    int FailedCount,
    int RejectedCount,
    int ApprovedCount,
    int ArchivedCount);

public sealed record MarketplaceProductStatusDto(
    Guid ProductId,
    string ProductTitle,
    string StatusText,
    string? StatusMessage,
    DateTimeOffset? LastSyncedAt);
