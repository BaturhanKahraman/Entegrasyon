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
                "english",
                p => new { p.Code, p.Name, p.TaxNumber })
            .HasIndex(p => p.SearchVector)
            .HasMethod("GIN");
        CargoCompany[] companies = new[]
        {
            new CargoCompany
            {
                Id = 1,
                Code = "MNG",
                Name = "MNG Kargo",
                TaxNumber = "123456",
                CreatedAt = DateTimeOffset.MinValue
            },new CargoCompany
            {
                Id = 2,
                Code = "YK",
                Name = "Yurtiçi Kargo",
                TaxNumber = "123456",
                CreatedAt = DateTimeOffset.MinValue
            },
            new CargoCompany
            {
                Id = 3,
                Code = "TEX",
                Name = "Trendyol Express",
                TaxNumber = "123456",
                CreatedAt = DateTimeOffset.MinValue
            },
            new CargoCompany
            {
                Id = 4,
                Code = "SK",
                Name = "Sürat Kargo",
                TaxNumber = "123456",
                CreatedAt = DateTimeOffset.MinValue
            }
        };
        builder.HasData(companies);
    }
}