using Entegrasyon.Entity.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class TemplateCategoryAttrMarketplaceMappingEntityConfiguration : IEntityTypeConfiguration<TemplateCategoryAttrMarketplaceMapping>
{
    public void Configure(EntityTypeBuilder<TemplateCategoryAttrMarketplaceMapping> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ExternalAttributeExternalId).HasMaxLength(200);

        builder.HasOne(x => x.TemplateCategoryAttributeData).WithMany(x => x.MarketplaceMappings).HasForeignKey(x => x.TemplateCategoryAttributeDataId);
    }
}
