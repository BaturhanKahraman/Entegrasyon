using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Entity.Dtos.Sale;

public record SaleListDetailDto(
    Guid Id,
    string SaleNumber,
    DateTimeOffset SaleDate,
    SaleSource SaleSource,
    SaleStatus SaleStatus,
    string? CustomerFullName,
    string SalePersonFullName,
    int SaleItemVarietyCount,
    int SaleItemCount,
    decimal GrandTotal,
    List<string> PaymentMethods);
