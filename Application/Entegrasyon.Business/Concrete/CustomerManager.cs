using System.Linq.Expressions;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.MapperProfiles;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Logs;
using FluentValidation;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Extensions;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Concrete;

public class CustomerManager : ICustomerManager
{
    private readonly IDbContextFactory<IntegrationDbContext> _contextFactory;
    private readonly IFluentValidator _fluentValidator;
    private readonly IMapper _mapper;
    private readonly IApplicationLogManager _applicationLogManager;

    public CustomerManager(IDbContextFactory<IntegrationDbContext> contextFactory, IFluentValidator fluentValidator, IMapper mapper, IApplicationLogManager applicationLogManager)
    {
        _contextFactory = contextFactory;
        _fluentValidator = fluentValidator;
        _mapper = mapper;
        _applicationLogManager = applicationLogManager;
    }

    public async Task<IDataResult<Customer>> UpdateCustomer(UpdateCustomerDto customerDto)
    {
        using var dbContext = _contextFactory.CreateDbContext();
        await _applicationLogManager.AddLog("Müşteri düzenleme isteği geldi.", LogType.Customer, LogAction.Update);
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
        await _applicationLogManager.AddLog("Müşteri düzenleme isteği başarılı oldu.", LogType.Customer, LogAction.Update);
        return new SuccessDataResult<Customer>(applicationCustomer!);
    }

    public async Task<IResult> AddCustomer(CustomerAddDto dto)
    {
        using var dbContext = _contextFactory.CreateDbContext();
        await _applicationLogManager.AddLog("Müşteri ekleme isteği geldi.", LogType.Customer, LogAction.Add);
        await _fluentValidator.ValidateAndThrowAsync(dto);
        Customer customer = dto.CustomerType == "Retail"
            ? _mapper.Map<RetailCustomer>(dto)
            : _mapper.Map<CorporateCustomer>(dto);
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync();
        await _applicationLogManager.AddLog("Müşteri ekleme isteği başarılı oldu.", LogType.Customer, LogAction.Add);
        CustomerDetailDto result;
        if (customer is RetailCustomer retailCustomer)
            result = _mapper.Map<CustomerDetailDto>(retailCustomer);
        else
            result = _mapper.Map<CustomerDetailDto>(customer);
        return new SuccessDataResult<CustomerDetailDto>(result, "Müşteri başarıyla eklendi.");
    }

    public async Task<IDataResult<Pageable<CustomerDetailDto>>> GetCustomerDetailPageable(string customerInfo, int pageIndex = 0, int itemCount = 50)
    {
        using var dbContext = _contextFactory.CreateDbContext();
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
        using var dbContext = _contextFactory.CreateDbContext();
        var result = await dbContext.Customers.AnyAsync(x => x.Id == id);
        return result ? new SuccessResult() : new ErrorResult("Müşteri bulunamamıştır.");
    }

    public async Task<IDataResult<IEnumerable<CustomerDetailDto>>> GetCustomerBySearch(string searchText)
    {
        using var dbContext = _contextFactory.CreateDbContext();
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
        using var dbContext = _contextFactory.CreateDbContext();
        return new SuccessDataResult<Customer>((await dbContext.Customers.FirstOrDefaultAsync(x => x.Id == id))!);
    }

    public async Task<IDataResult<CustomerDetailDto>> GetCustomerDetailById(int id)
    {
        using var dbContext = _contextFactory.CreateDbContext();
        var cust = await dbContext.Customers
            .Where(c => c.Id == id)
            .Select(FuncMappings.CustomerToDetailDto().ToExpression())
            .FirstOrDefaultAsync();
        return new SuccessDataResult<CustomerDetailDto>(cust!);
    }

    public async Task<IResult> SoftDelete(int id)
    {
        using var dbContext = _contextFactory.CreateDbContext();
        var cust = await dbContext.Customers.AsTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (cust == null)
            return new ErrorResult(Messages.CustomerNotFound);
        cust.IsDeleted = true;
        cust.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();
        return new SuccessResult(Messages.ProcessSuccess);
    }
}
