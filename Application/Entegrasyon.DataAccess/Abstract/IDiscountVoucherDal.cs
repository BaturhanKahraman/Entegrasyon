using Entegrasyon.Entity.Sales;
using Shared;

namespace Entegrasyon.DataAccess.Abstract;

public interface IDiscountVoucherDal : IEntityRepository<DiscountVoucher>
{
    Task ChangeStatus(IEnumerable<int> ids,bool status=false);
}