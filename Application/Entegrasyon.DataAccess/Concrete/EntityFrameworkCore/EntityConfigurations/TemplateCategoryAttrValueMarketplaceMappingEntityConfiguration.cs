using Entegrasyon.Entity.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class TemplateCategoryAttrValueMarketplaceMappingEntityConfiguration : IEntityTypeConfiguration<TemplateCategoryAttrValueMarketplaceMapping>
{
    public void Configure(EntityTypeBuilder<TemplateCategoryAttrValueMarketplaceMapping> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ExternalValueExternalId).HasMaxLength(200);

        builder.HasOne(x => x.TemplateCategoryAttributeValueData).WithMany(x => x.MarketplaceMappings).HasForeignKey(x => x.TemplateCategoryAttributeValueDataId);
    }
}
