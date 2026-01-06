using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Customers;
using Shared.DTO;
using Shared.Entity;
using Shared.Results;

namespace Entegrasyon.Business.Abstract;

public interface ICustomerManager
{
    Task<IDataResult<Customer>> UpdateCustomer(UpdateCustomerDto customerDto);
    Task<IResult> AddCustomer(CustomerAddDto dto);
    Task<IDataResult<Pageable<CustomerDetailDto>>> GetCustomerDetailPageable(string customerInfo, int pageIndex = 0, int itemCount = 50);
    Task<IResult> CheckIfCustomerExits(int id);
    Task<IDataResult<IEnumerable<CustomerDetailDto>>> GetCustomerBySearch(string searchText);
    Task<IDataResult<Customer>> GetCustomerById(int id);
    Task<IDataResult<CustomerDetailDto>> GetCustomerDetailById(int id);
    Task<IResult> SoftDelete(int id);
}
