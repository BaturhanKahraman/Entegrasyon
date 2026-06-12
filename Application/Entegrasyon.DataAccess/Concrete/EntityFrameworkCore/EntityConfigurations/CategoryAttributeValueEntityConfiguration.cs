using Entegrasyon.Entity.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public sealed class CategoryAttributeValueEntityConfiguration : IEntityTypeConfiguration<CategoryAttributeValue>
{
    public void Configure(EntityTypeBuilder<CategoryAttributeValue> builder)
    {
        builder.Property(x => x.NormalizedName)
            .HasMaxLength(256)
            .IsRequired();

        // Aktif (silinmemis) degerlerde attribute basina kanonik tekillik.
        builder.HasIndex(x => new { x.CategoryAttributeId, x.NormalizedName })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}
