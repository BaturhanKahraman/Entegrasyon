using Entegrasyon.Entity.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class PricingRuleEntityConfiguration : IEntityTypeConfiguration<PricingRule>
{
    public void Configure(EntityTypeBuilder<PricingRule> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Value).HasColumnType("numeric(18,2)");

        builder.HasIndex(x => new { x.MarketPlaceId, x.Scope, x.ScopeEntityId, x.IsActive })
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne(x => x.MarketPlace)
            .WithMany()
            .HasForeignKey(x => x.MarketPlaceId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
