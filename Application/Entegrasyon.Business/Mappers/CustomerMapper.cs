using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Customers;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class CustomerMapper
{
    // CustomerAddDto → polymorphic targets
    public partial RetailCustomer MapToRetail(CustomerAddDto dto);

    [MapProperty(nameof(CustomerAddDto.NationalIdentity), nameof(CorporateCustomer.TaxNumber))]
    public partial CorporateCustomer MapToCorporate(CustomerAddDto dto);
}
