namespace Entegrasyon.Entity.Dtos.Customers;

public enum CustomerActivitySource
{
    ApplicationLog = 1,
    Order = 2,
    Sale = 3,
    SaleReturn = 4,
    EInvoice = 5,
    DiscountVoucher = 6,
    Lifecycle = 7
}

public sealed class CustomerActivityDto
{
    public DateTimeOffset OccurredAt { get; init; }
    public CustomerActivitySource Source { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Icon { get; init; }
    public string? BadgeColor { get; init; }
    public string? RelatedUrl { get; init; }
    public string? UserDisplayName { get; init; }
    public string? Amount { get; init; }
}
