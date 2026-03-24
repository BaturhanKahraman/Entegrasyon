using Entegrasyon.Entity.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class TemplateBrandMarketplaceMappingEntityConfiguration : IEntityTypeConfiguration<TemplateBrandMarketplaceMapping>
{
    public void Configure(EntityTypeBuilder<TemplateBrandMarketplaceMapping> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ExternalBrandExternalId).HasMaxLength(200);

        builder.HasOne(x => x.TemplateBrandData).WithMany(x => x.MarketplaceMappings).HasForeignKey(x => x.TemplateBrandDataId);
    }
}
