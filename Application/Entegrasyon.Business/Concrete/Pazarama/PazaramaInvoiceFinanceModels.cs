using System.Text.Json.Serialization;

namespace Entegrasyon.Business.Concrete.Pazarama;

// -----------------------------------------------------------------------
// Invoice — Fatura linki yükleme
// -----------------------------------------------------------------------

/// <summary>
/// Tek sipariş/paket/item bazlı fatura yükleme isteği.
/// POST /order/invoice-link
/// deliveryCompanyId ve trackingNumber null ise tüm siparişe fatura yüklenir.
/// </summary>
public sealed record PazaramaInvoiceLinkRequest(
    [property: JsonPropertyName("invoiceLink")] string InvoiceLink,
    [property: JsonPropertyName("orderid")] string OrderId,
    [property: JsonPropertyName("orderItemId")] string? OrderItemId = null,
    [property: JsonPropertyName("deliveryCompanyId")] string? DeliveryCompanyId = null,
    [property: JsonPropertyName("trackingNumber")] string? TrackingNumber = null);

/// <summary>
/// Birden fazla item'a fatura yükleme isteği.
/// POST /order/multiple-invoice-link
/// </summary>
public sealed record PazaramaMultipleInvoiceLinkRequest(
    [property: JsonPropertyName("orderId")] string OrderId,
    [property: JsonPropertyName("invoiceLink")] string InvoiceLink,
    [property: JsonPropertyName("deliveryCompanyId")] string? DeliveryCompanyId = null,
    [property: JsonPropertyName("trackingNumber")] string? TrackingNumber = null,
    [property: JsonPropertyName("orderItemIds")] List<string> OrderItemIds = default!);

// -----------------------------------------------------------------------
// Finance — Muhasebe / Finans sorgusu
// -----------------------------------------------------------------------

/// <summary>
/// Finans sorgusu isteği.
/// POST /order/paymentAgreement
/// </summary>
public sealed record PazaramaFinanceRequest(
    [property: JsonPropertyName("startDate")] string StartDate,
    [property: JsonPropertyName("endDate")] string EndDate,
    [property: JsonPropertyName("allowanceDate")] string? AllowanceDate = null,
    [property: JsonPropertyName("orderId")] long? OrderId = null);

/// <summary>
/// Finans sorgusu yanıtındaki data objesi.
/// </summary>
public sealed record PazaramaFinanceData(
    [property: JsonPropertyName("transactionList")] List<PazaramaFinanceTransaction>? TransactionList,
    [property: JsonPropertyName("totalAmount")] decimal TotalAmount,
    [property: JsonPropertyName("totalCommission")] decimal TotalCommission,
    [property: JsonPropertyName("totalAllowance")] decimal TotalAllowance);

/// <summary>
/// Finans işlem satırı.
/// </summary>
public sealed record PazaramaFinanceTransaction(
    [property: JsonPropertyName("orderId")] long OrderId,
    [property: JsonPropertyName("trxCode")] string? TrxCode,
    [property: JsonPropertyName("trxId")] string? TrxId,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("installmentNumber")] int InstallmentNumber,
    [property: JsonPropertyName("commissionAmount")] decimal CommissionAmount,
    [property: JsonPropertyName("couponDiscount")] decimal CouponDiscount,
    [property: JsonPropertyName("allowanceAmount")] decimal AllowanceAmount,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("transactionDate")] string? TransactionDate,
    [property: JsonPropertyName("transferredDate")] string? TransferredDate);
