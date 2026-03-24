using Entegrasyon.Entity.Invoicing;

namespace Entegrasyon.Entity.Dtos.Invoicing;

public sealed record EInvoiceDetailDto(
    Guid Id,
    string InvoiceNumber,
    EInvoiceType InvoiceType,
    EInvoiceStatus Status,
    string CustomerTaxId,
    string CustomerTitle,
    DateTimeOffset IssueDate,
    decimal TotalAmount,
    decimal TaxAmount,
    decimal GrandTotal,
    string? GibUuid,
    string? PdfUrl,
    Guid? SaleId,
    Guid? MarketplaceOrderId,
    IntegratorProvider IntegratorProvider,
    string? ErrorMessage,
    DateTimeOffset? SentAt,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset? CancelledAt,
    List<EInvoiceLineDetailDto> Lines);

public sealed record EInvoiceLineDetailDto(
    Guid Id,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    int TaxRate,
    decimal TaxAmount,
    decimal LineTotal);
