using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Entity.Dtos.Sale;

public sealed record MakeSaleDto(Guid SalePersonId,int CustomerId,ICollection<SaleItem> SaleItems);
