using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Entity.Dtos.Sale;

public record SaleDetailDto
{
    public Guid Id { get; init; }
    public string SaleNumber { get; init; } = "";
    public DateTimeOffset SaleDate { get; init; }
    public SaleSource SaleSource { get; init; }
    public SaleStatus SaleStatus { get; init; }
    public string? CustomerName { get; init; }
    public string SalePersonName { get; init; } = "";
    public string BranchOfficeName { get; init; } = "";
    public string? Note { get; init; }
    public decimal SubTotal { get; init; }
    public decimal VatTotal { get; init; }
    public decimal GrandTotal { get; init; }
    public double GeneralDiscount { get; init; }
    public List<SaleDetailItemDto> Items { get; init; } = [];
    public List<SaleDetailPaymentDto> Payments { get; init; } = [];
    public List<VatSummaryLineDto> VatSummary { get; init; } = [];
    public List<SaleReturnSummaryDto> Returns { get; init; } = [];
}

public record SaleDetailItemDto
{
    public Guid Id { get; init; }
    public string ProductTitle { get; init; } = "";
    public string Barcode { get; init; } = "";
    public int Quantity { get; init; }
    public decimal UnitPriceWithVat { get; init; }
    public decimal VatRate { get; init; }
    public decimal LineTotalWithVat { get; init; }
    public int ReturnedQuantity { get; init; }
}

public record SaleDetailPaymentDto
{
    public string PaymentMethodName { get; init; } = "";
    public string PaymentMethodIcon { get; init; } = "";
    public decimal Amount { get; init; }
    public string? CardAuthCode { get; init; }
    public DateTimeOffset PaidAt { get; init; }
}

public record VatSummaryLineDto(
    decimal VatRate,
    decimal TaxBase,
    decimal VatAmount,
    decimal Total);
