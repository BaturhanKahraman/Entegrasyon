using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Customers;
using Shared.Entity;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Abstract;

public interface ICustomerDal : IEntityRepository<Customer>
{
    Task<IReadOnlyList<CustomerDetailDto>> GetCustomerDetailsAsync(string fullTextSearch);
    Task<Pageable<CustomerDetailDto>> GetCustomerDetailsPageable(string fullTextSearch, int pageIndex, int pageSize);
}