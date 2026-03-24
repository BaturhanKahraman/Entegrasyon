using Entegrasyon.Entity.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class TemplateCargoCompanyDataEntityConfiguration : IEntityTypeConfiguration<TemplateCargoCompanyData>
{
    public void Configure(EntityTypeBuilder<TemplateCargoCompanyData> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Code).HasMaxLength(100);

        builder.HasOne(x => x.Package).WithMany(x => x.CargoCompanies).HasForeignKey(x => x.PackageId);
        builder.HasMany(x => x.MarketplaceMappings).WithOne(x => x.TemplateCargoCompanyData).HasForeignKey(x => x.TemplateCargoCompanyDataId);
    }
}
