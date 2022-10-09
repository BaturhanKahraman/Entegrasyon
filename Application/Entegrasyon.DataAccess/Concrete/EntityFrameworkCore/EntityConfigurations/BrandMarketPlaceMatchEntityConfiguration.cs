using Entegrasyon.Entity.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class BrandMarketPlaceMatchEntityConfiguration:IEntityTypeConfiguration<BrandMarketPlaceMatch>
{
    public void Configure(EntityTypeBuilder<BrandMarketPlaceMatch> builder)
    {
        builder.HasKey(x => new { x.MarketPlaceId,x.ApplicationBrandId });
    }
}