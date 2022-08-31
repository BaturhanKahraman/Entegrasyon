using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Sales;
using Shared.Abstract.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfDiscountVoucherDal:EfEntityRepository<DiscountVoucher,IntegrationDbContext>,IDiscountVoucherDal
{
    public EfDiscountVoucherDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}