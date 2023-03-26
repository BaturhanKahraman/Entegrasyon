using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Dtos.Brand;
using Microsoft.EntityFrameworkCore;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfBrandDal: EfEntityRepository<Brand,IntegrationDbContext>, IBrandDal
{
    private readonly IntegrationDbContext _context;
    public EfBrandDal(IntegrationDbContext ctx) : base(ctx)
    {
        _context = ctx;
    }

    public async Task<BrandListDetailDto> ConvertToBrandDetail(Brand brand)
    {
        var dbBrand = await Table.FindAsync(brand.Id);
        if (dbBrand == null)
            throw new Exception("Veritabanı objesi bulunamadı.");
        int productCount = await _context.Entry(dbBrand).Collection(b => b.Products).Query().CountAsync();
        return new BrandListDetailDto(dbBrand.Id,dbBrand.CreatedAt,dbBrand.Name, productCount);
    }
}