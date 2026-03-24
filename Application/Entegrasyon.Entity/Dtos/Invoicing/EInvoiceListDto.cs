using Entegrasyon.Entity.Invoicing;

namespace Entegrasyon.Entity.Dtos.Invoicing;

public sealed record EInvoiceListDto(
    Guid Id,
    string InvoiceNumber,
    EInvoiceType InvoiceType,
    EInvoiceStatus Status,
    string CustomerTitle,
    DateTimeOffset IssueDate,
    decimal GrandTotal,
    IntegratorProvider IntegratorProvider);
