using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Customers;

namespace Entegrasyon.Business.MapperProfiles;
public static class FuncMappings
{
    public static Func<Customer,CustomerDetailDto> CustomerToDetailDto()
    {
        return x =>
        {
            return x switch
            {
                RetailCustomer retailCustomer => new CustomerDetailDto(retailCustomer.CreatedAt,retailCustomer.Id,
                    retailCustomer.NationalIdentity ?? "",retailCustomer.FullName ?? "",null!,retailCustomer.Sales?.Count() ?? 0,
                    retailCustomer.PhoneNumber ?? "",retailCustomer.Address?.FullAddress ?? "",
                    retailCustomer.CustomerType ?? ""),
                CorporateCustomer corporateCustomer => new CustomerDetailDto(corporateCustomer.CreatedAt,
                    corporateCustomer.Id,corporateCustomer.TaxNumber ?? "",corporateCustomer.FullName ?? "",
                    corporateCustomer.CorporateName ?? "",corporateCustomer.Sales?.Count() ?? 0,corporateCustomer.PhoneNumber ?? "",
                    corporateCustomer.Address?.FullAddress ?? "",corporateCustomer.CustomerType ?? ""),
                _ => new CustomerDetailDto(x.CreatedAt,x.Id,"","","",x.Sales?.Count() ?? 0,x.PhoneNumber ?? "",
                    x.Address?.FullAddress ?? "",x.CustomerType ?? "")
            };
        };
    }

    
}
