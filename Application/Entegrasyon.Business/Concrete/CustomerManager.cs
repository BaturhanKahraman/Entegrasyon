using System.Linq.Expressions;
using AutoMapper;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Logs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Entity;
using Shared.EntityFrameworkCore;
using Shared.Extensions;
using Shared.Logic;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class CustomerManager
{
    private readonly IApplicationCustomerDal _customerDal;
    private readonly FluentValidator _fluentValidator;
    private readonly IMapper _mapper;
    private readonly ApplicationLogManager _applicationLogManager;
    public CustomerManager(IApplicationCustomerDal customerDal, FluentValidator fluentValidator, IMapper mapper, ApplicationLogManager applicationLogManager)
    {
        _customerDal = customerDal;
        _fluentValidator = fluentValidator;
        _mapper = mapper;
        _applicationLogManager = applicationLogManager;
    }

    public async Task<IResult> UpdateCustomer(UpdateCustomerDto dto)
    {
        await _applicationLogManager.AddLog("Müşteri düzenleme isteği geldi.", LogType.Customer, LogAction.Update);
        await _fluentValidator.ValidateAndThrowAsync(dto);
        var result = LogicRunner.Run(await CheckIfSameIdExits(dto.NationalIdentity));
        if (result != null)
        {
            await _applicationLogManager.AddLog("Müşteri düzenleme isteği başarısız oldu. "+result.Message,LogType.Customer,LogAction.Update,dto);
            return result;

        }
        var applicationCustomer = _mapper.Map<Customer>(dto);
        await _customerDal.AddAsync(applicationCustomer);
        await _applicationLogManager.AddLog("Müşteri düzenleme isteği başarılı oldu.", LogType.Customer, LogAction.Update);
        return new SuccessResult();
    } 

    public async Task<IResult> AddCustomer(AddCustomerDto dto)
    {
        await _applicationLogManager.AddLog("Müşteri ekleme isteği geldi.", LogType.Customer, LogAction.Add);
        await _fluentValidator.ValidateAndThrowAsync(dto);
        var result = LogicRunner.Run(await CheckIfSameIdExits(dto.NationalIdentity));
        if (result != null)
        {
            await _applicationLogManager.AddLog("Müşteri ekleme isteği başarısız oldu. " + result.Message,LogType.Customer,LogAction.Add,dto);
            return result;
        }
        var applicationCustomer = _mapper.Map<Customer>(dto);
        await _customerDal.AddAsync(applicationCustomer);
        await _applicationLogManager.AddLog("Müşteri ekleme isteği başarılı oldu.", LogType.Customer, LogAction.Add);
        return new SuccessResult();
    }

    public async Task<IDataResult<Pageable<CustomerDetailDto>>> GetCustomerDetailPageable(string customerInfo,int pageIndex = 0,int itemCount = 50)
    {
        var orders = new List<(string,string)>
        {
            new ("Id","desc"),
            new ("Sales.Count","desc")
        };
        var filters = new List<(bool, Expression<Func<Customer,bool>>)>
        {
        };
        var pageableResult = await _customerDal.GetPaginatedTransformedEntities(pageIndex, itemCount,
            x=>new CustomerDetailDto(x.CreatedAt,x.Id,x.,x.Name,x.Surname,x.Sales.Count,x.PhoneNumber,x.Address),orders,filters);
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