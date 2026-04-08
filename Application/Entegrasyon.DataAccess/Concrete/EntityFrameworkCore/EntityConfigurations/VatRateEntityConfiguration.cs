using Entegrasyon.Entity.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class VatRateEntityConfiguration : IEntityTypeConfiguration<VatRate>
{
    public void Configure(EntityTypeBuilder<VatRate> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Rate).HasColumnType("numeric(5,2)");

        builder.HasIndex(x => x.Rate).IsUnique().HasFilter("\"IsDeleted\" = false");

        builder.HasData(
            new VatRate { Id = 1, Rate = 1, Name = "Indirimli KDV (%1)", Description = "Temel gida, tarim urunleri", IsDefault = false, IsActive = true },
            new VatRate { Id = 2, Rate = 10, Name = "Indirimli KDV (%10)", Description = "Gida, konut, tekstil", IsDefault = true, IsActive = true },
            new VatRate { Id = 3, Rate = 20, Name = "Standart KDV (%20)", Description = "Genel mal ve hizmetler", IsDefault = false, IsActive = true }
        );
    }
}
