using Entegrasyon.Entity.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class AttributeKeyValueEntityConfiguration : IEntityTypeConfiguration<AttributeKeyValue>
{
    public void Configure(EntityTypeBuilder<AttributeKeyValue> builder)
    {
        builder.HasKey(x => new {x.CategoryAttributeId,x.ProductId });
        builder.Property(x => x.CustomValue).IsRequired(false);
        builder.Property(x => x.AttributeValueId).IsRequired(false);
        builder.HasOne(x => x.CategoryAttribute).WithMany(ca => ca.AttributeKeyValues).HasForeignKey(x => x.CategoryAttributeId);
        builder.HasOne(x => x.Product).WithMany(pv => pv.AttributeKeyValues).HasForeignKey(x=>x.ProductId);
    }
}