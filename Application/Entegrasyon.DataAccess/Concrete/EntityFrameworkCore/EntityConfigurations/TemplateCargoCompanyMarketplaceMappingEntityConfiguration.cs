using Entegrasyon.Entity.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class TemplateCargoCompanyMarketplaceMappingEntityConfiguration : IEntityTypeConfiguration<TemplateCargoCompanyMarketplaceMapping>
{
    public void Configure(EntityTypeBuilder<TemplateCargoCompanyMarketplaceMapping> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.TemplateCargoCompanyData).WithMany(x => x.MarketplaceMappings).HasForeignKey(x => x.TemplateCargoCompanyDataId);
    }
}
