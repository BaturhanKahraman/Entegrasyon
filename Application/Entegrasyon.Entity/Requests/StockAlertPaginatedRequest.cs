using Entegrasyon.Entity.Dtos.Reports;

namespace Entegrasyon.Entity;

public record StockAlertPaginatedRequest() : PaginatedRequest()
{
    public int MinimumStockThreshold { get; init; } = 10;

    /// <summary>Şube filtresi — null = tüm şubeler. Çok-şubeli esnaf için.</summary>
    public int? BranchOfficeId { get; init; }

    /// <summary>Alert seviye filtresi — null = tüm seviyeler ("sadece kritikleri göster").</summary>
    public StockAlertLevel? AlertLevel { get; init; }
}
