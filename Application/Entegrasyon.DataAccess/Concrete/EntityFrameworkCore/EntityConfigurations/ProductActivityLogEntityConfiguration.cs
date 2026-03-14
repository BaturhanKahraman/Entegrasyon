using Entegrasyon.Entity.Logs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class ProductActivityLogEntityConfiguration : IEntityTypeConfiguration<ProductActivityLog>
{
    public void Configure(EntityTypeBuilder<ProductActivityLog> builder)
    {
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => new { x.ProductId, x.CreatedAt });
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
