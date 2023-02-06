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
        builder.Property(x => x.ImportId).IsRequired(false);
        builder.HasIndex(x => x.ImportId);
        Category[] categories = new Category[6];
        categories[0] = new Category { CreatedAt = DateTimeOffset.MinValue, Id = 1, IsFavorite = false, Name = "Giyim" };
        categories[1] = new Category { CreatedAt = DateTimeOffset.MinValue, Id = 2, IsFavorite = false, Name = "Teknoloji" };
        categories[2] = new Category { CreatedAt = DateTimeOffset.MinValue, Id = 3, IsFavorite = false, Name = "Hayat" };
        categories[3] = new Category { CreatedAt = DateTimeOffset.MinValue, Id = 4, IsFavorite = false, Name = "Ev Eşyaları" };
        categories[4] = new Category { CreatedAt = DateTimeOffset.MinValue,Id = 5,IsFavorite = false,Name = "Çocuk Giyim",SuperCategoryId = 1};
        categories[5] = new Category { CreatedAt = DateTimeOffset.MinValue,Id = 6,IsFavorite = true,Name = "Çocuk Ceket",SuperCategoryId = 5};

        builder.HasData(categories);
    }
}