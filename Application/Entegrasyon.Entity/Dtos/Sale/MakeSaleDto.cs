namespace Entegrasyon.Entity.Dtos.Sale;

public sealed record MakeSaleDto(
    Guid SalePersonId,
    int CustomerId,
    double GeneralDiscount,
    ICollection<SaleItemDto> SaleItems);
public sealed record SaleItemDto(
    Guid ProductVariantId,
    double TaxPercentage,
    double DiscountPercent,
    decimal UnitPrice,
    int Quantity,
    string DiscountVoucherCode);