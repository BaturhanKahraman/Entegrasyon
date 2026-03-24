using Entegrasyon.Entity.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class TemplateCategoryAttributeValueDataEntityConfiguration : IEntityTypeConfiguration<TemplateCategoryAttributeValueData>
{
    public void Configure(EntityTypeBuilder<TemplateCategoryAttributeValueData> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ValueName).IsRequired().HasMaxLength(500);

        builder.HasOne(x => x.TemplateCategoryAttributeData).WithMany(x => x.Values).HasForeignKey(x => x.TemplateCategoryAttributeDataId);
        builder.HasMany(x => x.MarketplaceMappings).WithOne(x => x.TemplateCategoryAttributeValueData).HasForeignKey(x => x.TemplateCategoryAttributeValueDataId);
    }
}
