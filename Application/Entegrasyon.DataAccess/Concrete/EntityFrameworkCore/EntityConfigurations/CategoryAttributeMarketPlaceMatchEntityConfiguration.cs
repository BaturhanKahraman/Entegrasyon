using Entegrasyon.Entity.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class CategoryAttributeMarketPlaceMatchEntityConfiguration: IEntityTypeConfiguration<CategoryAttributeMarketPlaceMatch>
{
    public void Configure(EntityTypeBuilder<CategoryAttributeMarketPlaceMatch> builder)
    {
        builder.HasKey(x => new {x.MarketPlaceId,x.ApplicationCategoryAttributeId});
        
    }
}