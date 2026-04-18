using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Entity.Dtos.Sale;

public sealed record MakeSaleDto(
    Guid SalePersonId,
    int? CustomerId,
    decimal GeneralDiscount,
    int BranchOfficeId,
    SaleSource SaleSource,
    string? Note,
    IEnumerable<SaleItemDto> SaleItems,
    List<SalePaymentDto> Payments,
    int? GeneralDiscountReasonId = null,
    string? GeneralDiscountReasonNote = null);

public sealed record SaleItemDto(
    Guid ProductVariantId,
    double TaxPercentage,
    double DiscountPercent,
    decimal UnitPrice,
    int Quantity,
    string DiscountVoucherCode,
    decimal? DiscountAmount = null,
    int? DiscountReasonId = null,
    string? DiscountReasonNote = null);
