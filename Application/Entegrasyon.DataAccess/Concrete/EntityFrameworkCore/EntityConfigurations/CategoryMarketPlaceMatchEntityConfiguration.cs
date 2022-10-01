using Entegrasyon.Entity.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class CategoryMarketPlaceMatchEntityConfiguration : IEntityTypeConfiguration<CategoryMarketPlaceMatch>
{
    public void Configure(EntityTypeBuilder<CategoryMarketPlaceMatch> builder)
    {
        builder.HasKey(x => new { x.MarketPlaceId,x.ApplicationCategoryId });
    }
}