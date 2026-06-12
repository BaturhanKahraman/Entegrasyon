using Entegrasyon.Entity.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class AttributeKeyValueEntityConfiguration : IEntityTypeConfiguration<AttributeKeyValue>
{
    public void Configure(EntityTypeBuilder<AttributeKeyValue> builder)
    {
        builder.HasKey(x => new {x.CategoryAttributeId,x.ProductId });
        builder.Property(x => x.AttributeValueId).IsRequired(true);
        builder.HasOne(x => x.Product).WithMany(pv => pv.AttributeKeyValues).HasForeignKey(x=>x.ProductId);
        // AttributeValue FK: Restrict (Cascade DEĞİL). Değerler soft-delete edilir; bir CategoryAttributeValue
        // fiziksel silinirse ürün-özellik linklerini sessizce cascade-silmesin — SaveChanges'te hata versin.
        builder.HasOne(x => x.AttributeValue).WithMany().HasForeignKey(x => x.AttributeValueId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}