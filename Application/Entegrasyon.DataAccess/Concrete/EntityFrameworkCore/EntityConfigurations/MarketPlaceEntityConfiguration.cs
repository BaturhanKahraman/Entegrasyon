using Entegrasyon.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class MarketPlaceEntityConfiguration:IEntityTypeConfiguration<MarketPlace>
{
    public void Configure(EntityTypeBuilder<MarketPlace> builder)
    {
        builder.HasData(new List<MarketPlace>()
        {
            new MarketPlace
            {
                Id=1,
                Name = "Trendyol"

            }
        });
    }
}