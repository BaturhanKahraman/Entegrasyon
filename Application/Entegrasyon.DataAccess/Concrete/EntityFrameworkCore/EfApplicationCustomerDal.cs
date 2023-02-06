using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Customers;
using Microsoft.EntityFrameworkCore;
using Shared.Entity;
using Shared.EntityFrameworkCore;
using Shared.Extensions;
using Entegrasyon.Entity.Customers;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfApplicationCustomerDal : EfEntityRepository<Customer, IntegrationDbContext>, ICustomerDal
{
    private readonly IntegrationDbContext _dbContext;
    public EfApplicationCustomerDal(IntegrationDbContext ctx) : base(ctx)
    {
        _dbContext = ctx;
    }

    public async Task<IReadOnlyList<CustomerDetailDto>> GetCustomerDetailsAsync(string fullTextSearch)
    {
        return (await _dbContext.Customers
            .WhereIf(!string.IsNullOrEmpty(fullTextSearch),
            x =>
            (x as RetailCustomer).RetailSearchVector.Matches(EF.Functions.ToTsQuery(fullTextSearch.ToFullTextSearchQuery()))
            ||
            (x as CorporateCustomer).CorporateSearchVector.Matches(EF.Functions.ToTsQuery(fullTextSearch.ToFullTextSearchQuery()))
            ).AsSingleQuery().AsNoTracking()
            .ToListAsync())
            .Select(CustomerToDetailDto())
            .ToList().AsReadOnly();
    }

    public async Task<Pageable<CustomerDetailDto>> GetCustomerDetailsPageable(string fullTextSearch,int pageIndex,int pageSize)
    {
        var customerQueryable = _dbContext.Customers
            .WhereIf(!string.IsNullOrEmpty(fullTextSearch),
            x =>
            (x as RetailCustomer).RetailSearchVector.Matches(EF.Functions.ToTsQuery(fullTextSearch.ToFullTextSearchQuery()))
            ||
            (x as CorporateCustomer).CorporateSearchVector.Matches(EF.Functions.ToTsQuery(fullTextSearch.ToFullTextSearchQuery()))
            ).AsSplitQuery().AsNoTracking();
        var customers =await customerQueryable.ToPageableQuery(pageIndex, pageSize).ToListAsync();
        var customerCount = await customerQueryable.CountAsync();
        return new Pageable<CustomerDetailDto>(customers.Select(CustomerToDetailDto()), pageIndex, pageSize, customerCount);
    }

    private static Func<Customer, CustomerDetailDto> CustomerToDetailDto()
    {
        return x =>
        {
            return x switch
            {
                RetailCustomer retailCustomer => new CustomerDetailDto(retailCustomer.CreatedAt, retailCustomer.Id,
                    retailCustomer.NationalIdentity, retailCustomer.FullName, null, retailCustomer.Sales?.Count() ?? 0,
                    retailCustomer.PhoneNumber, retailCustomer.Address?.FullAddress ?? "",
                    retailCustomer.CustomerType),
                CorporateCustomer corporateCustomer => new CustomerDetailDto(corporateCustomer.CreatedAt,
                    corporateCustomer.Id, corporateCustomer.TaxNumber, corporateCustomer.FullName,
                    corporateCustomer.CorporateName, corporateCustomer.Sales?.Count() ?? 0, corporateCustomer.PhoneNumber,
                    corporateCustomer.Address?.FullAddress ?? "", corporateCustomer.CustomerType),
                _ => new CustomerDetailDto(x.CreatedAt, x.Id, "", "", "", x.Sales?.Count() ?? 0, x.PhoneNumber,
                    x.Address?.FullAddress ?? "", x.CustomerType)
            };
        };
    }
}