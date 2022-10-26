using AutoMapper;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Logs;
using Shared.Entity;
using Shared.Logic;
using Shared.Results;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;

namespace Entegrasyon.Business.Concrete;

public class CustomerManager
{
    private readonly IApplicationCustomerDal _customerDal;
    private readonly FluentValidator _fluentValidator;
    private readonly IMapper _mapper;
    private readonly ApplicationLogManager _applicationLogManager;
    public CustomerManager(IApplicationCustomerDal customerDal,FluentValidator fluentValidator,IMapper mapper,ApplicationLogManager applicationLogManager)
    {
        _customerDal = customerDal;
        _fluentValidator = fluentValidator;
        _mapper = mapper;
        _applicationLogManager = applicationLogManager;
    }

    public async Task<IResult> UpdateCustomer(UpdateCustomerDto dto)
    {
        await _applicationLogManager.AddLog("Müşteri düzenleme isteği geldi.",LogType.Customer,LogAction.Update);
        await _fluentValidator.ValidateAndThrowAsync(dto);
        var result = LogicRunner.Run(await CheckIfSameIdExits(dto.NationalIdentity));
        if(result != null)
        {
            await _applicationLogManager.AddLog("Müşteri düzenleme isteği başarısız oldu. " + result.Message,LogType.Customer,LogAction.Update,dto);
            return result;

        }
        var applicationCustomer = _mapper.Map<Customer>(dto);
        await _customerDal.AddAsync(applicationCustomer);
        await _applicationLogManager.AddLog("Müşteri düzenleme isteği başarılı oldu.",LogType.Customer,LogAction.Update);
        return new SuccessResult();
    }

    public async Task<IResult> AddCustomer(AddCustomerDto dto)
    {
        await _applicationLogManager.AddLog("Müşteri ekleme isteği geldi.",LogType.Customer,LogAction.Add);
        await _fluentValidator.ValidateAndThrowAsync(dto);
        var result = LogicRunner.Run(await CheckIfSameIdExits(dto.NationalIdentity));
        if(result != null)
        {
            await _applicationLogManager.AddLog("Müşteri ekleme isteği başarısız oldu. " + result.Message,LogType.Customer,LogAction.Add,dto);
            return result;
        }
        var applicationCustomer = _mapper.Map<Customer>(dto);
        await _customerDal.AddAsync(applicationCustomer);
        await _applicationLogManager.AddLog("Müşteri ekleme isteği başarılı oldu.",LogType.Customer,LogAction.Add);
        return new SuccessResult();
    }

    public async Task<IDataResult<Pageable<CustomerDetailDto>>> GetCustomerDetailPageable(string customerInfo,int pageIndex = 0,int itemCount = 50)
    {
        List<CustomerDetailDto> customerDetailDtos = new();
        (await _customerDal.Table.Skip(pageIndex * itemCount).Take(itemCount).AsNoTracking().ToListAsync()).ForEach(x =>
        {
            switch (x)
            {
                case RetailCustomer retail:
                    customerDetailDtos.Add(new CustomerDetailDto(retail.CreatedAt, retail.Id, retail.NationalIdentity,
                        retail.FullName, retail.Sales.Count, retail.PhoneNumber, retail.Address?.FullAddress ?? "",
                        "Retail"));
                    break;
                case CorporateCustomer corporate:
                    customerDetailDtos.Add(new CustomerDetailDto(corporate.CreatedAt, corporate.Id, corporate.TaxNumber,
                        corporate.CorporateName, corporate.Sales.Count, corporate.PhoneNumber,
                        corporate.Address?.FullAddress ?? "", "Corporate"));
                    break;
            }
        });
        var totalItemCount = await _customerDal.Table.CountAsync();


        return new SuccessDataResult<Pageable<CustomerDetailDto>>(customerDetailDtos.ToPageable(pageIndex,itemCount,totalItemCount));
    }

    private async Task<IResult> CheckIfSameIdExits(string customerIdentity)
    {
        if(string.IsNullOrEmpty(customerIdentity))
            return new SuccessResult();
        //if (await _customerDal.Exists(x => x.NationalIdentity == customerIdentity))
        //    return new ErrorResult("Aynı id'de başka bir müşteri var.");
        return new SuccessResult();
    }

}