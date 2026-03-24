using Entegrasyon.Entity.Invoicing;

namespace Entegrasyon.Entity.Dtos.Invoicing;

public sealed record EInvoiceFilterDto
{
    public int PageIndex { get; init; }
    public int PageSize { get; init; } = 20;
    public EInvoiceStatus? Status { get; init; }
    public EInvoiceType? InvoiceType { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public string? SearchTerm { get; init; }
}
