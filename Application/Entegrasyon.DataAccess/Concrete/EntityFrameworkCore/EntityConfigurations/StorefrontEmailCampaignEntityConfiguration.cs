using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontEmailCampaignEntityConfiguration : IEntityTypeConfiguration<StorefrontEmailCampaign>
{
    public void Configure(EntityTypeBuilder<StorefrontEmailCampaign> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => new { x.TenantId, x.Status });

        builder.Property(x => x.Subject).IsRequired().HasMaxLength(500);
        builder.Property(x => x.HtmlContent).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Target).HasConversion<string>().HasMaxLength(30);
    }
}
