using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Brands;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class BrandEntityConfiguration:IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.Property(b => b.Name).IsRequired();
        builder.HasMany(x => x.Products)
            .WithOne(x => x.Brand)
            .HasForeignKey(x => x.BrandId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.SeoSlug).IsUnique().HasFilter("\"SeoSlug\" IS NOT NULL");

        //Brand[] brands = new Brand[]
        //{
        //    new Brand { Id = 1,Name = "Nike",CreatedAt = DateTimeOffset.MinValue },
        //    new Brand { Id = 2,Name = "Adidas",CreatedAt = DateTimeOffset.MinValue },
        //    new Brand { Id = 3,Name = "Puma",CreatedAt = DateTimeOffset.MinValue },
        //    new Brand { Id = 4,Name = "Reebok",CreatedAt = DateTimeOffset.MinValue }
        //}; 
        //builder.HasData(brands);
    }
}