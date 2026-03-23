using System.Text.Json.Serialization;

namespace Entegrasyon.Business.Concrete.Trendyol.EFatura;

// ── Auth Models ──────────────────────────────────────────────────────────

public sealed record EFaturaPartnerSignInRequest(string Email, string Password);

public sealed record EFaturaCustomerSignInRequest(string Email, string Password, string TaxId);

public sealed record EFaturaCustomerSignInResponse(
    int UserId,
    int CompanyId,
    int PartnerCustomerId,
    string AccessToken);

// ── Taxpayer Query ───────────────────────────────────────────────────────

public sealed record EFaturaTaxPayerInfo(
    string? TaxId,
    string? Alias,
    string? Title,
    string? AliasType);

// ── Invoice Create ───────────────────────────────────────────────────────

public sealed record EFaturaCreateRequest
{
    [JsonPropertyName("autoInvoiceId")]
    public bool AutoInvoiceId { get; init; } = true;

    [JsonPropertyName("companyId")]
    public int CompanyId { get; init; }

    [JsonPropertyName("userId")]
    public int UserId { get; init; }

    [JsonPropertyName("source")]
    public string Source { get; init; } = "PARTNER";

    [JsonPropertyName("localReferenceId")]
    public string? LocalReferenceId { get; init; }

    [JsonPropertyName("notes")]
    public List<string>? Notes { get; init; }

    [JsonPropertyName("issuedAt")]
    public string? IssuedAt { get; init; }

    [JsonPropertyName("recipientInfo")]
    public EFaturaRecipientInfo RecipientInfo { get; init; } = null!;

    [JsonPropertyName("currencyInfo")]
    public EFaturaCurrencyInfo CurrencyInfo { get; init; } = new("TRY", false);

    [JsonPropertyName("invoiceInfo")]
    public EFaturaInvoiceInfo InvoiceInfo { get; init; } = null!;

    [JsonPropertyName("invoiceLines")]
    public List<EFaturaInvoiceLine> InvoiceLines { get; init; } = [];

    [JsonPropertyName("totalTax")]
    public EFaturaTotalTax TotalTax { get; init; } = null!;

    [JsonPropertyName("invoiceTotal")]
    public EFaturaInvoiceTotal InvoiceTotal { get; init; } = null!;

    [JsonPropertyName("paymentInfo")]
    public EFaturaPaymentInfo? PaymentInfo { get; init; }

    [JsonPropertyName("deliveryInfo")]
    public EFaturaDeliveryInfo? DeliveryInfo { get; init; }

    [JsonPropertyName("orderInfo")]
    public EFaturaOrderInfo? OrderInfo { get; init; }

    /// <summary>Sadece e-fatura icin — alici etiket bilgisi</summary>
    [JsonPropertyName("targetAlias")]
    public string? TargetAlias { get; init; }
}

public sealed record EFaturaRecipientInfo(
    [property: JsonPropertyName("taxId")] string? TaxId,
    [property: JsonPropertyName("countryCode")] string CountryCode,
    [property: JsonPropertyName("city")] string? City,
    [property: JsonPropertyName("district")] string? District,
    [property: JsonPropertyName("address")] string? Address,
    [property: JsonPropertyName("postalCode")] string? PostalCode,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("surname")] string? Surname,
    [property: JsonPropertyName("taxOffice")] string? TaxOffice);

public sealed record EFaturaCurrencyInfo(
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("hasExchange")] bool HasExchange);

public sealed record EFaturaInvoiceInfo(
    [property: JsonPropertyName("invoiceType")] string InvoiceType,
    [property: JsonPropertyName("invoiceTypeCode")] string InvoiceTypeCode);

public sealed record EFaturaInvoiceLine
{
    [JsonPropertyName("unitCode")]
    public string UnitCode { get; init; } = "C62";

    [JsonPropertyName("quantity")]
    public int Quantity { get; init; }

    [JsonPropertyName("totalAmount")]
    public long TotalAmount { get; init; }

    [JsonPropertyName("taxAmount")]
    public long TaxAmount { get; init; }

    [JsonPropertyName("taxableAmount")]
    public long TaxableAmount { get; init; }

    [JsonPropertyName("taxPercent")]
    public int TaxPercent { get; init; }

    [JsonPropertyName("taxName")]
    public string TaxName { get; init; } = "KDV";

    [JsonPropertyName("taxCode")]
    public string TaxCode { get; init; } = "0015";

    [JsonPropertyName("itemName")]
    public string? ItemName { get; init; }

    [JsonPropertyName("unitPriceAmount")]
    public long UnitPriceAmount { get; init; }

    [JsonPropertyName("totalDiscountAmount")]
    public long TotalDiscountAmount { get; init; }

    [JsonPropertyName("totalTax")]
    public EFaturaTotalTax TotalTax { get; init; } = null!;
}

public sealed record EFaturaTotalTax(
    [property: JsonPropertyName("totalTaxAmount")] long TotalTaxAmount,
    [property: JsonPropertyName("subTotalTaxes")] List<EFaturaSubTotalTax> SubTotalTaxes);

public sealed record EFaturaSubTotalTax(
    [property: JsonPropertyName("taxableAmount")] long TaxableAmount,
    [property: JsonPropertyName("taxAmount")] long TaxAmount,
    [property: JsonPropertyName("taxType")] string TaxType,
    [property: JsonPropertyName("percent")] int Percent);

public sealed record EFaturaInvoiceTotal(
    [property: JsonPropertyName("lineExtensionAmount")] long LineExtensionAmount,
    [property: JsonPropertyName("taxExclusiveAmount")] long TaxExclusiveAmount,
    [property: JsonPropertyName("taxInclusiveAmount")] long TaxInclusiveAmount,
    [property: JsonPropertyName("payableAmount")] long PayableAmount,
    [property: JsonPropertyName("allowanceTotalAmount")] long AllowanceTotalAmount);

public sealed record EFaturaPaymentInfo(
    [property: JsonPropertyName("purchaseUrl")] string? PurchaseUrl,
    [property: JsonPropertyName("paymentMeans")] string PaymentMeans,
    [property: JsonPropertyName("paymentDate")] string? PaymentDate);

public sealed record EFaturaDeliveryInfo(
    [property: JsonPropertyName("carrierTaxId")] string? CarrierTaxId,
    [property: JsonPropertyName("carrierName")] string? CarrierName,
    [property: JsonPropertyName("sentAt")] string? SentAt);

public sealed record EFaturaOrderInfo(
    [property: JsonPropertyName("orderId")] string? OrderId,
    [property: JsonPropertyName("orderDate")] string? OrderDate);

// ── Invoice Create Response ──────────────────────────────────────────────

public sealed record EFaturaCreateResponse
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("invoiceUuid")]
    public string? InvoiceUuid { get; init; }

    [JsonPropertyName("invoiceId")]
    public string? InvoiceId { get; init; }

    [JsonPropertyName("status")]
    public int Status { get; init; }

    [JsonPropertyName("scenario")]
    public string? Scenario { get; init; }

    [JsonPropertyName("invoiceTypeCode")]
    public string? InvoiceTypeCode { get; init; }

    [JsonPropertyName("payableAmount")]
    public long PayableAmount { get; init; }

    [JsonPropertyName("taxAmount")]
    public long TaxAmount { get; init; }

    [JsonPropertyName("envelopeId")]
    public string? EnvelopeId { get; init; }
}

// ── Status Response ──────────────────────────────────────────────────────

public sealed record EFaturaStatusResponse(
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("gibStatus")] string? GibStatus,
    [property: JsonPropertyName("invoiceUuid")] string? InvoiceUuid);

// ── Cancel Request ───────────────────────────────────────────────────────

public sealed record EFaturaCancelRequest(
    [property: JsonPropertyName("invoiceUuid")] string InvoiceUuid,
    [property: JsonPropertyName("companyId")] int CompanyId);

// ── Download Request ─────────────────────────────────────────────────────

public sealed record EFaturaDownloadRequest(
    [property: JsonPropertyName("documentType")] string DocumentType,
    [property: JsonPropertyName("fileExtension")] string FileExtension,
    [property: JsonPropertyName("documentUuid")] string DocumentUuid,
    [property: JsonPropertyName("companyId")] int CompanyId);

// ── Remaining Credit Response ────────────────────────────────────────────

public sealed record EFaturaRemainingCreditResponse(
    [property: JsonPropertyName("taxId")] string? TaxId,
    [property: JsonPropertyName("remainingCredit")] int RemainingCredit,
    [property: JsonPropertyName("remainingCreditStatus")] string? RemainingCreditStatus);
