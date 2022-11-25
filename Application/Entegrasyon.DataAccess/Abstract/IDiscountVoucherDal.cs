using Entegrasyon.Entity.DiscountVouchers;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Abstract;

public interface IDiscountVoucherDal : IEntityRepository<DiscountVoucher>
{
    Task ChangeStatus(IEnumerable<int> ids,bool status=false);
}