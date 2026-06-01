using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontAddressManager
{
    Task<IDataResult<List<StorefrontAddress>>> GetCustomerAddressesAsync(int tenantId, int customerId);
    Task<IDataResult<StorefrontAddress>> GetByIdAsync(int tenantId, int customerId, int addressId);
    Task<IResult> AddAsync(int tenantId, int customerId, StorefrontAddressDto dto);
    Task<IResult> UpdateAsync(int tenantId, int customerId, int addressId, StorefrontAddressDto dto);
    Task<IResult> DeleteAsync(int tenantId, int customerId, int addressId);
    Task<IResult> SetDefaultAsync(int tenantId, int customerId, int addressId);
}
