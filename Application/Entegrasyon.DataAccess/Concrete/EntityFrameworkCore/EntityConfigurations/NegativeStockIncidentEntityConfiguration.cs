using Entegrasyon.Entity.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class NegativeStockIncidentEntityConfiguration : IEntityTypeConfiguration<NegativeStockIncident>
{
    public void Configure(EntityTypeBuilder<NegativeStockIncident> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.Resolution });
        builder.HasIndex(x => x.ProductVariantId);

        builder.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.TriggeringSource).HasMaxLength(50);
        builder.Property(x => x.TriggeringReferenceId).HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(1000);
    }
}
