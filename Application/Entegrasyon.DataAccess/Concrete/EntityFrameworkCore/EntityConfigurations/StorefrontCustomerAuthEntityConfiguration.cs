using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class StorefrontCustomerAuthEntityConfiguration : IEntityTypeConfiguration<StorefrontCustomerAuth>
{
    public void Configure(EntityTypeBuilder<StorefrontCustomerAuth> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.CustomerId });

        builder.Property(x => x.Email).IsRequired().HasMaxLength(200);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
