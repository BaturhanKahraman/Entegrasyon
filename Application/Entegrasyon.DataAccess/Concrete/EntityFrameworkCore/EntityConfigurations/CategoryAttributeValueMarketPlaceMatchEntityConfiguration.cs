using Entegrasyon.Entity;
using Entegrasyon.Entity.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class CategoryAttributeValueMarketPlaceMatchEntityConfiguration: IEntityTypeConfiguration<CategoryAttributeValueMarketPlaceMatch>
{

    public void Configure(EntityTypeBuilder<CategoryAttributeValueMarketPlaceMatch> builder)
    {
        builder.HasKey(x => new {x.MarketPlaceId,x.ApplicationCategoryAttributeValueId});
    }
}