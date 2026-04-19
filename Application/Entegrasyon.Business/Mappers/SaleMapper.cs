using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Sales;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class SaleMapper
{
    [MapProperty(nameof(SaleItem.UsedDiscountVoucherCode), nameof(SaleItemDto.DiscountVoucherCode))]
    public partial SaleItemDto MapToDto(SaleItem item);

    [MapProperty(nameof(SaleItemDto.DiscountVoucherCode), nameof(SaleItem.UsedDiscountVoucherCode))]
    [MapperIgnoreTarget(nameof(SaleItem.ProductVariant))]
    [MapperIgnoreTarget(nameof(SaleItem.BranchOffice))]
    [MapperIgnoreTarget(nameof(SaleItem.DiscountReason))]
    public partial SaleItem MapToEntity(SaleItemDto dto);

    [MapperIgnoreTarget(nameof(Sale.DiscountVoucher))]
    [MapperIgnoreTarget(nameof(Sale.SalePerson))]
    [MapperIgnoreTarget(nameof(Sale.BranchOffice))]
    [MapperIgnoreTarget(nameof(Sale.Customer))]
    [MapperIgnoreTarget(nameof(Sale.Payments))]
    [MapperIgnoreTarget(nameof(Sale.Returns))]
    [MapperIgnoreTarget(nameof(Sale.SaleNumber))]
    [MapperIgnoreTarget(nameof(Sale.SaleDate))]
    [MapperIgnoreTarget(nameof(Sale.SaleStatus))]
    [MapperIgnoreTarget(nameof(Sale.GeneralDiscountReason))]
    public partial Sale MapToEntity(MakeSaleDto dto);

    public partial MakeSaleDto MapToDto(Sale sale);
}
