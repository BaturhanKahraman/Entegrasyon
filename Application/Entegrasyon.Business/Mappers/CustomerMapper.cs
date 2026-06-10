using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Customers;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class CustomerMapper
{
    // CustomerAddDto → polymorphic targets.
    // Yeni create formu tip-toggle ile ilgili alanı kendi adıyla gönderir
    // (bireysel → NationalIdentity, kurumsal → TaxNumber). Mapperly aynı-isimli
    // scalar'ları otomatik eşler; TaxNumber → TaxNumber doğrudan map'lenir.
    // (Eski form vergi no'yu NationalIdentity alanında gönderiyor + override ile
    // TaxNumber'a taşıyordu; toggle form bu gereksiz dolayıyı kaldırdı.)
    public partial RetailCustomer MapToRetail(CustomerAddDto dto);

    public partial CorporateCustomer MapToCorporate(CustomerAddDto dto);
}
