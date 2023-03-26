using Entegrasyon.Entity.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class CategoryAttributeCategoryEntityConfigurations: IEntityTypeConfiguration<CategoryAttributeCategory>
{
    public void Configure(EntityTypeBuilder<CategoryAttributeCategory> builder)
    {
        builder.HasKey(x => new { x.CategoryId,x.CategoryAttributeId });
        builder.HasOne(x => x.Category).WithMany(x => x.CategoryAttributes).HasForeignKey(x => x.CategoryId);
        builder.HasOne(x=> x.CategoryAttribute).WithMany(x => x.Categories).HasForeignKey(x=>x.CategoryAttributeId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}