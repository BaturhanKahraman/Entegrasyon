using Entegrasyon.Entity.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class CargoCompanyMarketPlaceMatchEntityConfiguration:IEntityTypeConfiguration<CargoCompanyMarketPlaceMatch>
{
    public void Configure(EntityTypeBuilder<CargoCompanyMarketPlaceMatch> builder)
    {
        builder.HasKey(x => new { x.MarketPlaceId,x.ApplicationCargoCompanyId });
    }
}