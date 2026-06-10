using Entegrasyon.Entity.Help;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class HelpRequestEntityConfiguration : IEntityTypeConfiguration<HelpRequest>
{
    public void Configure(EntityTypeBuilder<HelpRequest> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        // Admin liste: tenant filtreli + CreatedAt'e gore tersten siralama (yeni → eski).
        builder.HasIndex(x => new { x.TenantId, x.CreatedAt });
        // Acik/cozuldu filtresi icin.
        builder.HasIndex(x => new { x.TenantId, x.Status });

        builder.Property(x => x.Subject).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Message).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.Category).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();
    }
}
