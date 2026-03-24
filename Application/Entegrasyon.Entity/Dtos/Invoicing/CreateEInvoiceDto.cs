using Entegrasyon.Entity.Invoicing;

namespace Entegrasyon.Entity.Dtos.Invoicing;

public sealed record CreateEInvoiceDto
{
    public EInvoiceType InvoiceType { get; init; }
    public string CustomerTaxId { get; init; } = string.Empty;
    public string CustomerTitle { get; init; } = string.Empty;
    public DateTimeOffset IssueDate { get; init; }
    public IntegratorProvider IntegratorProvider { get; init; }
    public Guid? SaleId { get; init; }
    public Guid? MarketplaceOrderId { get; init; }
    public List<CreateEInvoiceLineDto> Lines { get; init; } = [];
}

public sealed record CreateEInvoiceLineDto(
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    int TaxRate);
