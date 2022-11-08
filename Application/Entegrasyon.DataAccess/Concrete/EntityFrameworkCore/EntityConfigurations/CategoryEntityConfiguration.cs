using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Categories;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class CategoryEntityConfiguration:IEntityTypeConfiguration<Category>
{
    

    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasOne(x => x.SuperCategory)
            .WithMany(x=>x.SubCategories)
            .HasForeignKey(x=>x.SuperCategoryId);

    }
}