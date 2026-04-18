using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Customers;

namespace Entegrasyon.Business.Mappers;

public static class FuncMappings
{
    public static Func<Customer, CustomerDetailDto> CustomerToDetailDto()
    {
        return x => x switch
        {
            RetailCustomer r => new CustomerDetailDto(
                r.CreatedAt, r.Id, r.NationalIdentity ?? "", r.FullName ?? "", null!,
                r.Sales?.Count() ?? 0, r.PhoneNumber ?? "", r.Address?.FullAddress ?? "",
                r.CustomerType ?? "", r.IsActive, r.DeactivatedAt, r.DeactivationReason),
            CorporateCustomer c => new CustomerDetailDto(
                c.CreatedAt, c.Id, c.TaxNumber ?? "", c.FullName ?? "",
                c.CorporateName ?? "", c.Sales?.Count() ?? 0, c.PhoneNumber ?? "",
                c.Address?.FullAddress ?? "", c.CustomerType ?? "",
                c.IsActive, c.DeactivatedAt, c.DeactivationReason),
            _ => new CustomerDetailDto(
                x.CreatedAt, x.Id, "", "", "", x.Sales?.Count() ?? 0, x.PhoneNumber ?? "",
                x.Address?.FullAddress ?? "", x.CustomerType ?? "",
                x.IsActive, x.DeactivatedAt, x.DeactivationReason)
        };
    }
}
