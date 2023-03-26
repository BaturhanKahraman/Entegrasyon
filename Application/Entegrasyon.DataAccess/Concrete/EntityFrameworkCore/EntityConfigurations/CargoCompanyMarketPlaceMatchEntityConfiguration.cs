using Entegrasyon.Entity.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class CargoCompanyMarketPlaceMatchEntityConfiguration:IEntityTypeConfiguration<CargoCompanyMarketPlaceMatch>
{
    public void Configure(EntityTypeBuilder<CargoCompanyMarketPlaceMatch> builder)
    {
        builder.HasKey(x => new { x.MarketPlaceId,x.ApplicationCargoCompanyId });
        CargoCompanyMarketPlaceMatch[] companyMatches = {
            new() {ApplicationCargoCompanyId = 1,MarketPlaceId = 1,MarketPlaceCargoCompanyId = 42},
            new() {ApplicationCargoCompanyId = 2,MarketPlaceId = 1,MarketPlaceCargoCompanyId = 38},
            new() {ApplicationCargoCompanyId = 3,MarketPlaceId = 1,MarketPlaceCargoCompanyId = 36},
            new() {ApplicationCargoCompanyId = 4,MarketPlaceId = 1,MarketPlaceCargoCompanyId = 34},
            new() {ApplicationCargoCompanyId = 5,MarketPlaceId = 1,MarketPlaceCargoCompanyId = 39},
            new() {ApplicationCargoCompanyId = 6,MarketPlaceId = 1,MarketPlaceCargoCompanyId = 35},
            new() {ApplicationCargoCompanyId = 7,MarketPlaceId = 1,MarketPlaceCargoCompanyId = 30},
            new() {ApplicationCargoCompanyId = 8,MarketPlaceId = 1,MarketPlaceCargoCompanyId = 12},
            new() {ApplicationCargoCompanyId = 9,MarketPlaceId = 1,MarketPlaceCargoCompanyId = 13},
            new() {ApplicationCargoCompanyId = 10,MarketPlaceId = 1,MarketPlaceCargoCompanyId = 14},
            new() {ApplicationCargoCompanyId = 11,MarketPlaceId = 1,MarketPlaceCargoCompanyId = 10},
            new() {ApplicationCargoCompanyId = 12,MarketPlaceId = 1,MarketPlaceCargoCompanyId = 19},
            new() {ApplicationCargoCompanyId = 13,MarketPlaceId = 1,MarketPlaceCargoCompanyId = 9},
            new() {ApplicationCargoCompanyId = 14,MarketPlaceId = 1,MarketPlaceCargoCompanyId = 17},
            new() {ApplicationCargoCompanyId = 15,MarketPlaceId = 1,MarketPlaceCargoCompanyId = 6},
            new() {ApplicationCargoCompanyId = 16,MarketPlaceId = 1,MarketPlaceCargoCompanyId = 20},
            new() {ApplicationCargoCompanyId = 17,MarketPlaceId = 1,MarketPlaceCargoCompanyId = 4},
            new() {ApplicationCargoCompanyId = 18,MarketPlaceId = 1,MarketPlaceCargoCompanyId = 7},
       };
        builder.HasData(companyMatches);
    }
}