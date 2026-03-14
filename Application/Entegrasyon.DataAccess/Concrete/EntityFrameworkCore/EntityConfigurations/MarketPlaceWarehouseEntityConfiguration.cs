using Entegrasyon.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class MarketPlaceWarehouseEntityConfiguration : IEntityTypeConfiguration<MarketPlaceWarehouse>
{
    public void Configure(EntityTypeBuilder<MarketPlaceWarehouse> builder)
    {
        builder.HasIndex(x => new { x.MarketPlaceId, x.BranchOfficeId }).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.MarketPlace)
            .WithMany()
            .HasForeignKey(x => x.MarketPlaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.BranchOffice)
            .WithMany()
            .HasForeignKey(x => x.BranchOfficeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
