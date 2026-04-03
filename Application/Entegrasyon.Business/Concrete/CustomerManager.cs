using System.Linq.Expressions;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Logs;
using FluentValidation;
using Entegrasyon.Business.Mappers;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Extensions;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Concrete;

public class CustomerManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IFluentValidator fluentValidator,
    CustomerMapper mapper,
    IApplicationLogManager applicationLogManager) : ICustomerManager
{

    public async Task<IDataResult<Customer>> UpdateCustomer(UpdateCustomerDto customerDto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        await applicationLogManager.AddLog("Müşteri düzenleme isteği geldi.", LogType.Customer, LogAction.Update);
        Customer applicationCustomer = (await dbContext.Customers.AsTracking().FirstOrDefaultAsync(x => x.Id == customerDto.Id))!;
        if (customerDto.CustomerType == "Retail")
        {
            if (applicationCustomer is CorporateCustomer corpCustomer)
            {
                corpCustomer.CorporateName = null;
                corpCustomer.TaxNumber = null;
            }
            var retailCustomer = applicationCustomer as RetailCustomer;
            retailCustomer!.NationalIdentity = customerDto.NationalIdentity;
            retailCustomer.Name = customerDto.Name;
            retailCustomer.CustomerType = customerDto.CustomerType;
            retailCustomer.Surname = customerDto.Surname;
            retailCustomer.PhoneNumber = customerDto.PhoneNumber;
        }
        else
        {
            if (applicationCustomer is RetailCustomer retailCust)
                retailCust.NationalIdentity = null;
            var corporateCustomer = applicationCustomer as CorporateCustomer;
            corporateCustomer!.TaxNumber = customerDto.TaxNumber;
            corporateCustomer.CorporateName = customerDto.CorporateName;
            corporateCustomer.Name = customerDto.Name;
            corporateCustomer.CustomerType = customerDto.CustomerType;
            corporateCustomer.Surname = customerDto.Surname;
            corporateCustomer.PhoneNumber = customerDto.PhoneNumber;
        }
        await dbContext.SaveChangesAsync();
        await applicationLogManager.AddLog("Müşteri düzenleme isteği başarılı oldu.", LogType.Customer, LogAction.Update);
        return new SuccessDataResult<Customer>(applicationCustomer!);
    }

    public async Task<IResult> AddCustomer(CustomerAddDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        await applicationLogManager.AddLog("Müşteri ekleme isteği geldi.", LogType.Customer, LogAction.Add);
        await fluentValidator.ValidateAndThrowAsync(dto);
        Customer customer = dto.CustomerType == "Retail"
            ? mapper.MapToRetail(dto)
            : mapper.MapToCorporate(dto);
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();
        await applicationLogManager.AddLog("Müşteri ekleme isteği başarılı oldu.", LogType.Customer, LogAction.Add);
        var result = FuncMappings.CustomerToDetailDto()(customer);
        return new SuccessDataResult<CustomerDetailDto>(result, "Müşteri başarıyla eklendi.");
    }

    public async Task<IDataResult<Pageable<CustomerDetailDto>>> GetCustomerDetailPageable(string customerInfo, int pageIndex = 0, int itemCount = 50)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var query = dbContext.Customers.AsQueryable();
        if (!string.IsNullOrEmpty(customerInfo))
            query = query.Where(x =>
                (x as RetailCustomer)!.RetailSearchVector.Matches(EF.Functions.ToTsQuery(customerInfo.ToFullTextSearchQuery()))
                || (x as CorporateCustomer)!.CorporateSearchVector.Matches(EF.Functions.ToTsQuery(customerInfo.ToFullTextSearchQuery()))
                || x.FullName!.Contains(customerInfo));

        int total = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.FullName)
            .Skip(pageIndex * itemCount)
            .Take(itemCount)
            .Select(FuncMappings.CustomerToDetailDto().ToExpression())
            .ToListAsync();

        return new SuccessDataResult<Pageable<CustomerDetailDto>>(new Pageable<CustomerDetailDto>(items, pageIndex, itemCount, total));
    }

    public async Task<IResult> CheckIfCustomerExits(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var result = await dbContext.Customers.AnyAsync(x => x.Id == id);
        return result ? new SuccessResult() : new ErrorResult("Müşteri bulunamamıştır.");
    }

    public async Task<IDataResult<IEnumerable<CustomerDetailDto>>> GetCustomerBySearch(string searchText)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        if (string.IsNullOrEmpty(searchText))
            throw new ValidationException("Arama kriteri boş olamaz.");

        var result = await dbContext.Customers
            .Where(x =>
                (x as RetailCustomer)!.RetailSearchVector.Matches(EF.Functions.ToTsQuery(searchText.ToFullTextSearchQuery()))
                || (x as CorporateCustomer)!.CorporateSearchVector.Matches(EF.Functions.ToTsQuery(searchText.ToFullTextSearchQuery())))
            .OrderBy(x => x.FullName)
            .Select(FuncMappings.CustomerToDetailDto().ToExpression())
            .ToListAsync();
        return new SuccessDataResult<IEnumerable<CustomerDetailDto>>(result);
    }

    public async Task<IDataResult<Customer>> GetCustomerById(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return new SuccessDataResult<Customer>((await dbContext.Customers.FirstOrDefaultAsync(x => x.Id == id))!);
    }

    public async Task<IDataResult<CustomerDetailDto>> GetCustomerDetailById(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var cust = await dbContext.Customers
            .Where(c => c.Id == id)
            .Select(FuncMappings.CustomerToDetailDto().ToExpression())
            .FirstOrDefaultAsync();
        return new SuccessDataResult<CustomerDetailDto>(cust!);
    }

    public async Task<IResult> SoftDelete(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var cust = await dbContext.Customers.AsTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (cust == null)
            return new ErrorResult(Messages.CustomerNotFound);
        cust.IsDeleted = true;
        cust.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();
        return new SuccessResult(Messages.ProcessSuccess);
    }
}
