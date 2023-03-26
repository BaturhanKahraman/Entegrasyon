namespace Entegrasyon.Entity.Dtos.Sale;

public record SaleListDetailDto(
    Guid Id,
    DateTimeOffset SaleDate,
    bool IsDiscountApplied,
    double GeneralDiscount,
    string SalePersonFullName,
    string CustomerFullName,
    int SaleItemVarietyCount,
    int SaleItemCount,
    decimal TotalPrice
    );