using System.Linq.Expressions;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.MapperProfiles;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Logs;
using FluentValidation;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Shared.Entity;
using Shared.Extensions;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class CustomerManager
{
    private readonly ICustomerDal _customerDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFluentValidator _fluentValidator;
    private readonly IMapper _mapper;
    private readonly IApplicationLogManager _applicationLogManager;
    public CustomerManager(ICustomerDal customerDal,IFluentValidator fluentValidator,IMapper mapper,IApplicationLogManager applicationLogManager, IUnitOfWork unitOfWork)
    {
        _customerDal = customerDal;
        _fluentValidator = fluentValidator;
        _mapper = mapper;
        _applicationLogManager = applicationLogManager;
        _unitOfWork = unitOfWork;
    }

    public async Task<IDataResult<Customer>> UpdateCustomer(UpdateCustomerDto customerDto)
     {
        await _applicationLogManager.AddLog("Müşteri düzenleme isteği geldi.",LogType.Customer,LogAction.Update);
        //await _fluentValidator.ValidateAndThrowAsync(dto);

        Customer applicationCustomer = await _customerDal.GetAsync(x=>x.Id==customerDto.Id,true);
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
            if(applicationCustomer is RetailCustomer corpCustomer)
            {
                corpCustomer.NationalIdentity = null;
            }
            var corporateCustomer = applicationCustomer as CorporateCustomer;
            corporateCustomer!.TaxNumber = customerDto.TaxNumber;
            corporateCustomer.CorporateName = customerDto.CorporateName;
            corporateCustomer.Name = customerDto.Name;
            corporateCustomer.CustomerType = customerDto.CustomerType;
            corporateCustomer.Surname = customerDto.Surname;
            corporateCustomer.PhoneNumber = customerDto.PhoneNumber;
        }

        await _unitOfWork.SaveAsync();
        await _applicationLogManager.AddLog("Müşteri düzenleme isteği başarılı oldu.",LogType.Customer,LogAction.Update);
        return new SuccessDataResult<Customer>(applicationCustomer);
    }

    public async Task<IResult> AddCustomer(CustomerAddDto dto)
    {
        await _applicationLogManager.AddLog("Müşteri ekleme isteği geldi.",LogType.Customer,LogAction.Add);
        await _fluentValidator.ValidateAndThrowAsync(dto);
        Customer customer = dto.CustomerType == "Retail"
            ? _mapper.Map<RetailCustomer>(dto)
            : _mapper.Map<CorporateCustomer>(dto);
        await _customerDal.AddAsync(customer);
        await _applicationLogManager.AddLog("Müşteri ekleme isteği başarılı oldu.",LogType.Customer,LogAction.Add);
        CustomerDetailDto result;
        if(customer is RetailCustomer retailCustomer)
            result = _mapper.Map<CustomerDetailDto>(retailCustomer);
        else
            result = _mapper.Map<CustomerDetailDto>(customer);
        return new SuccessDataResult<CustomerDetailDto>(result, "Müşteri başarıyla eklendi.");
    }

    public async Task<IDataResult<Pageable<CustomerDetailDto>>> GetCustomerDetailPageable(string customerInfo,int pageIndex = 0,int itemCount = 50)
    {
        var result = await _customerDal.GetCustomerDetailsPageable(customerInfo,pageIndex,itemCount);
        return new SuccessDataResult<Pageable<CustomerDetailDto>>(result);
    }

    public async Task<IResult> CheckIfCustomerExits(int id)
    {
        var result = await _customerDal.Exists(x => x.Id == id);
        return result ? new SuccessResult() : new ErrorResult("Müşteri bulunamamıştır.");
    }

    private async Task<IResult> CheckIfSameIdExits(UpdateCustomerDto customer)
    {
        if (customer.CustomerType=="Retail")
        {

            var exits = await _customerDal.Table.Cast<RetailCustomer>()
                .AnyAsync(r => r.NationalIdentity == customer.NationalIdentity);
            return exits ? new ErrorResult(Messages.NationalIdentityAlreadyExits) : new SuccessResult();
        }
        if(customer.CustomerType=="Corporate")
        {
            var exits = await _customerDal.Table.Cast<CorporateCustomer>()
                .AnyAsync(r => r.TaxNumber == customer.NationalIdentity);
            return exits ? new ErrorResult(Messages.TaxNumberAlreadyExits) : new SuccessResult();
        }

        return new SuccessResult();
    }

    public async Task<IDataResult<IEnumerable<CustomerDetailDto>>> GetCustomerBySearch(string searchText)
    {
        if(string.IsNullOrEmpty(searchText))
            throw new ValidationException("Arama kriteri boş olamaz.");
        var orders = new List<(string, string)>
        {
            ("FullName", "asc")
        };

        Expression<Func<Customer,bool>> expression =
            x =>
                (x as RetailCustomer).RetailSearchVector.Matches(EF.Functions.ToTsQuery(searchText.ToFullTextSearchQuery()))
                ||
                (x as CorporateCustomer).CorporateSearchVector.Matches(EF.Functions.ToTsQuery(searchText.ToFullTextSearchQuery()));
        var result = await _customerDal.GetTransformedEntitiesAsync(FuncMappings.CustomerToDetailDto().ToExpression(),orders,expression);
        return new SuccessDataResult<IEnumerable<CustomerDetailDto>>(result);
    }

    public async Task<IDataResult<Customer>> GetCustomerById(int id)
    {
        return new SuccessDataResult<Customer>(await _customerDal.GetAsync(x => x.Id==id));
    }

    public async Task<IDataResult<CustomerDetailDto>> GetCustomerDetailById(int id)
    {
        Expression<Func<Customer, CustomerDetailDto>> expr = FuncMappings.CustomerToDetailDto().ToExpression();
        var cust = await _customerDal.GetTransformedEntity(expr,
            c=>c.Id==id);
        return new SuccessDataResult<CustomerDetailDto>(cust);
    }

    public async Task<IResult> SoftDelete(int id)
    {
        var cust =await _customerDal.GetAsync(x => x.Id == id);
        if (cust == null)
            return new ErrorResult(Messages.CustomerNotFound);
        await _customerDal.SoftDeleteAsync(cust);
        return new SuccessResult(Messages.ProcessSuccess);
    }
}