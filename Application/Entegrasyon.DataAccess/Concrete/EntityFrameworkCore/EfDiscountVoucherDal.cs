using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.DiscountVouchers;
using Microsoft.EntityFrameworkCore;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfDiscountVoucherDal:EfEntityRepository<DiscountVoucher,IntegrationDbContext>,IDiscountVoucherDal
{
    private readonly IntegrationDbContext _ctx;
    public EfDiscountVoucherDal(IntegrationDbContext ctx) : base(ctx)
    {
        _ctx = ctx;
    }

    public async Task ChangeStatus(IEnumerable<int> ids,bool status=false)
    {
        await _ctx.DiscountVouchers.Where(x => ids.Contains(x.Id) && x.IsActive)
            .ForEachAsync((x) => x.IsActive = status);
        await _ctx.SaveChangesAsync();
    }
}