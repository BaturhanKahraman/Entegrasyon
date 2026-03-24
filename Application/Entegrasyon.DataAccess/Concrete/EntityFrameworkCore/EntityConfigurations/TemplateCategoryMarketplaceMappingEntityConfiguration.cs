using Entegrasyon.Entity.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class TemplateCategoryMarketplaceMappingEntityConfiguration : IEntityTypeConfiguration<TemplateCategoryMarketplaceMapping>
{
    public void Configure(EntityTypeBuilder<TemplateCategoryMarketplaceMapping> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ExternalCategoryId).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ExternalCategoryName).HasMaxLength(500);

        builder.HasOne(x => x.TemplateCategoryData).WithMany(x => x.MarketplaceMappings).HasForeignKey(x => x.TemplateCategoryDataId);
    }
}
