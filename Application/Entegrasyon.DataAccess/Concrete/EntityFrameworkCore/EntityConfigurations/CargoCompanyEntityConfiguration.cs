using Entegrasyon.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class CargoCompanyEntityConfiguration : IEntityTypeConfiguration<CargoCompany>
{
    public void Configure(EntityTypeBuilder<CargoCompany> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(15);
        builder.Property(x => x.Name).HasMaxLength(50).IsRequired();
        builder.Property(x => x.TaxNumber).HasMaxLength(25);
        builder
            .HasGeneratedTsVectorColumn(
                p => p.SearchVector,
                "turkish",
                p => new { p.Code,p.Name,p.TaxNumber })
            .HasIndex(p => p.SearchVector)
            .HasMethod("GIN");
    }
}