using Entegrasyon.Entity.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class TemplateBrandDataEntityConfiguration : IEntityTypeConfiguration<TemplateBrandData>
{
    public void Configure(EntityTypeBuilder<TemplateBrandData> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(500);

        builder.HasOne(x => x.Package).WithMany(x => x.Brands).HasForeignKey(x => x.PackageId);
        builder.HasMany(x => x.MarketplaceMappings).WithOne(x => x.TemplateBrandData).HasForeignKey(x => x.TemplateBrandDataId);
    }
}
