using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Categories;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class CategoryEntityConfiguration:IEntityTypeConfiguration<Category>
{
    

    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(255)
            .IsRequired();
        builder.HasIndex(x => x.Name);
        builder.HasOne(x => x.SuperCategory)
            .WithMany(x=>x.SubCategories)
            .HasForeignKey(x=>x.SuperCategoryId);
        builder.Property(x => x.ImportId).IsRequired(false);
        builder.HasIndex(x => x.ImportId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}