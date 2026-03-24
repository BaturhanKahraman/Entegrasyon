using Entegrasyon.Entity.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class TemplateCategoryAttributeDataEntityConfiguration : IEntityTypeConfiguration<TemplateCategoryAttributeData>
{
    public void Configure(EntityTypeBuilder<TemplateCategoryAttributeData> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AttributeKey).IsRequired().HasMaxLength(500);
        builder.Property(x => x.AttributeHumanized).HasMaxLength(500);

        builder.HasOne(x => x.TemplateCategoryData).WithMany(x => x.Attributes).HasForeignKey(x => x.TemplateCategoryDataId);
        builder.HasMany(x => x.MarketplaceMappings).WithOne(x => x.TemplateCategoryAttributeData).HasForeignKey(x => x.TemplateCategoryAttributeDataId);
        builder.HasMany(x => x.Values).WithOne(x => x.TemplateCategoryAttributeData).HasForeignKey(x => x.TemplateCategoryAttributeDataId);
    }
}
