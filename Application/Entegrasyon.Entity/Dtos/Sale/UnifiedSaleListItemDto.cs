using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Entity.Dtos.Sale;

public sealed record UnifiedSaleListItemDto
{
    public Guid Id { get; init; }
    public int EntityType { get; init; }
    public UnifiedSaleSource Source { get; init; }
    public string? Number { get; init; }
    public DateTimeOffset SaleDate { get; init; }
    public string? CustomerDisplayName { get; init; }
    public string? CustomerSubLine { get; init; }
    public decimal TotalPrice { get; init; }
    public int ItemCount { get; init; }
    public UnifiedSaleStatus Status { get; init; }
    public string? StatusSubLine { get; init; }
    public string DetailUrl { get; init; } = "";
}
