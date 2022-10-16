using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Sales;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfImageDal : EfEntityRepository<Image,IntegrationDbContext>, IImageDal
{
    private readonly IntegrationDbContext _ctx;
    public EfImageDal(IntegrationDbContext ctx) : base(ctx)
    {
        _ctx = ctx;
    }

    public async Task AddRange(IEnumerable<Image> imgs)
    {
        await _ctx.Images.AddRangeAsync(imgs);
        await _ctx.SaveChangesAsync();
    }
}