using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity;

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
    Task<IResult> SetActive(int id, bool active, string? reason = null);
    Task<IDataResult<List<CustomerActivityDto>>> GetCustomerActivity(int id, int maxItems = 200);

    /// <summary>
    /// Müşteriler liste sayfası üst KPI kartları için global snapshot (bireysel/kurumsal/aktif
    /// sayıları). Tek server-side GroupBy(CustomerType), N+1 yok.
    /// </summary>
    Task<CustomerKpiDto> GetCustomerKpisAsync(CancellationToken ct = default);
}
