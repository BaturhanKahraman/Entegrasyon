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
        builder.HasQueryFilter(x => !x.IsDeleted);
        CargoCompany[] companies = new[]
        {
            new CargoCompany{Code="DHLMP",Id=1,Name="DHL Marketplace",TaxNumber ="951-241-77-13" },
            new CargoCompany{Code="SENDEOMP",Id=2,Name="NetKargo Lojistik Marketplace",TaxNumber ="2910804196" },
            new CargoCompany{Code="NETMP",Id=3,Name="DHL Marketplace",TaxNumber ="6930094440" },
            new CargoCompany{Code="MARSMP",Id=4,Name="Mars Lojistik Marketplace",TaxNumber ="6120538808" },
            new CargoCompany{Code="BIRGUNDEMP",Id=5,Name="Bir Günde Kargo Marketplace",TaxNumber ="1770545653" },
            new CargoCompany{Code="OCTOMP",Id=6,Name="Octovan Lojistik Marketplace",TaxNumber ="6330506845" },
            new CargoCompany{Code="BORMP",Id=7,Name="Borusan Lojistik Marketplace",TaxNumber ="1800038254" },
            new CargoCompany{Code="UPSMP",Id=8,Name="UPS Kargo Marketplace",TaxNumber ="9170014856" },
            new CargoCompany{Code="AGTMP",Id=9,Name="AGT Marketplace",TaxNumber ="6090414309" },
            new CargoCompany{Code="CAIMP",Id=10,Name="Cainiao Marketplace",TaxNumber ="0" },
            new CargoCompany{Code="MNGMP",Id=11,Name="MNG Kargo Marketplace",TaxNumber ="6080712084" },
            new CargoCompany{Code="PTTMP",Id=12,Name="PTT Kargo Marketplace",TaxNumber ="7320068060" },
            new CargoCompany{Code="SURATMP",Id=13,Name="Sürat Kargo Marketplace",TaxNumber ="7870233582" },
            new CargoCompany{Code="TEXMP",Id=14,Name="Trendyol Express Marketplace",TaxNumber ="8590921777" },
            new CargoCompany{Code="HOROZMP",Id=15,Name="Horoz Kargo Marketplace",TaxNumber ="4630097122" },
            new CargoCompany{Code="CEVAMP",Id=16,Name="CEVA Marketplace",TaxNumber ="8450298557" },
            new CargoCompany{Code="YKMP",Id=17,Name="Yurtiçi Kargo Marketplace",TaxNumber ="3130557669" },
            new CargoCompany{Code="ARASMP",Id=18,Name="Aras Kargo Marketplace",TaxNumber ="720039666" },
        };
        builder.HasData(companies);
    }
}