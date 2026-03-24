using Entegrasyon.Entity.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class MatchedEntityPackageEntityConfiguration : IEntityTypeConfiguration<MatchedEntityPackage>
{
    public void Configure(EntityTypeBuilder<MatchedEntityPackage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.EntityType).IsRequired();
        builder.Property(x => x.Version).HasDefaultValue(1);

        builder.HasMany(x => x.Categories).WithOne(x => x.Package).HasForeignKey(x => x.PackageId);
        builder.HasMany(x => x.Brands).WithOne(x => x.Package).HasForeignKey(x => x.PackageId);
        builder.HasMany(x => x.CargoCompanies).WithOne(x => x.Package).HasForeignKey(x => x.PackageId);
    }
}
