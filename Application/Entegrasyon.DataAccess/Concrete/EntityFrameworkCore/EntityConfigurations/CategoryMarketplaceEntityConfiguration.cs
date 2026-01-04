using Entegrasyon.Entity.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class CategoryMarketplaceEntityConfiguration : IEntityTypeConfiguration<CategoryMarketplace>
{
    public void Configure(EntityTypeBuilder<CategoryMarketplace> builder)
    {
        builder.HasKey(x => new { x.CategoryId, x.MarketPlaceId });
        builder.HasOne(x => x.Category).WithMany(x => x.MarketplaceLinks).HasForeignKey(x => x.CategoryId);
        builder.HasOne(x => x.MarketPlace).WithMany().HasForeignKey(x => x.MarketPlaceId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
