using AutoMapper;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Customers;
using Microsoft.EntityFrameworkCore;
using Shared.Entity;
using Shared.Extensions;
using Shared.Logic;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class CustomerManager
{
    private readonly IApplicationCustomerDal _customerDal;
    private readonly FluentValidator _fluentValidator;
    private readonly IMapper _mapper;
    public CustomerManager(IApplicationCustomerDal customerDal, FluentValidator fluentValidator, IMapper mapper)
    {
        _customerDal = customerDal;
        _fluentValidator = fluentValidator;
        _mapper = mapper;
    }

    public async Task<IResult> UpdateCustomer(UpdateCustomerDto dto)
    {
        await _fluentValidator.ValidateAndThrowAsync(dto);
        var result = LogicRunner.Run(await CheckIfSameIdExits(dto.Identity));
        if(result != null)
            return result;
        var applicationCustomer = _mapper.Map<ApplicationCustomer>(dto);
        await _customerDal.AddAsync(applicationCustomer);
        return new SuccessResult();
    } 

    public async Task<IResult> AddCustomer(AddCustomerDto dto)
    {
        await _fluentValidator.ValidateAndThrowAsync(dto);
        var result = LogicRunner.Run(await CheckIfSameIdExits(dto.Identity));
        if (result != null)
            return result;
        var applicationCustomer = _mapper.Map<ApplicationCustomer>(dto);
        await _customerDal.AddAsync(applicationCustomer);
        return new SuccessResult();
    }
    public async Task<IDataResult<Pageable<CustomerDetailDto>>> GetCustomerDetailPageable(int page = 1,int itemCount = 50,string customerInfo = null)
    {
        var items = _customerDal.Table
            .OrderByDescending(x => x.Sales.Count)
            .WhereIf(!string.IsNullOrEmpty(customerInfo), x =>
                x.SearchVector.Matches(EF.Functions.ToTsQuery(customerInfo.ForFullTextSearch())));
        var result = await items
            .Skip((page - 1) * itemCount).Take(itemCount)
            .Select(x => new CustomerDetailDto(x.NationalIdentity,x.Name,x.Surname,x.Sales.Count))
            .ToListAsync();
        int totalItemCount = await items.CountAsync();
        var pageableResult = new Pageable<CustomerDetailDto>(result,page,itemCount,totalItemCount,Convert.ToInt32(Math.Ceiling(totalItemCount / (double)itemCount)));
        return new SuccessDataResult<Pageable<CustomerDetailDto>>(pageableResult);
    }

    private async Task<IResult> CheckIfSameIdExits(string customerIdentity)
    {
        if (string.IsNullOrEmpty(customerIdentity))
            return new SuccessResult();
        if (await _customerDal.Exists(x => x.NationalIdentity == customerIdentity))
            return new ErrorResult("Aynı id'de başka bir müşteri var.");
        return new SuccessResult();
    }
    
}