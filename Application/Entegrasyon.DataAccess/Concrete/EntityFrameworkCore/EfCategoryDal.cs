using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.EntityFrameworkCore;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfCategoryDal: EfEntityRepository<Category,IntegrationDbContext>,ICategoryDal
{
    private readonly IntegrationDbContext _context;
    public EfCategoryDal(IntegrationDbContext ctx) : base(ctx)
    {
        _context = ctx;
    }

    public async Task UpdateRangeAsync(IEnumerable<Category> categories)
    {
        _context.Categories.UpdateRange(categories);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<CategoryDetailDto> ConvertToCategoryDetail(Category category)
    {
        var dbCategory = await Table.FindAsync(category.Id);
        if (dbCategory == null)
            throw new Exception("Veritabanı objesi bulunamadı.");

        int productCount= await _context.Entry(dbCategory).Collection(x => x.Products).Query().CountAsync();
        int catAttrCount= await _context.Entry(dbCategory).Collection(x => x.CategoryAttributes).Query().CountAsync();
        int subCategoryCount= await _context.Entry(dbCategory).Collection(x => x.SubCategories).Query().CountAsync();
        
        return new CategoryDetailDto(dbCategory.Id,
            productCount,
            dbCategory.Name,
            subCategoryCount,
            dbCategory.IsFavorite,
            catAttrCount);
    }
}