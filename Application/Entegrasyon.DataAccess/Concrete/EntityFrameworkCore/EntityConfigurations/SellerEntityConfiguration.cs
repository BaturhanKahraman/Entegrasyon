using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class SellerEntityConfiguration : IEntityTypeConfiguration<Seller>
{
    public void Configure(EntityTypeBuilder<Seller> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => new { x.TenantId, x.StoreSlug }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.CustomerId }).IsUnique();

        builder.Property(x => x.StoreName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.StoreSlug).HasMaxLength(200);
        builder.Property(x => x.CompanyName).IsRequired().HasMaxLength(300);
        builder.Property(x => x.TaxNumber).IsRequired().HasMaxLength(20);
        builder.Property(x => x.TaxOffice).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Iban).HasMaxLength(34);
        builder.Property(x => x.ContactPhone).IsRequired().HasMaxLength(20);
        builder.Property(x => x.ContactEmail).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Address).IsRequired().HasMaxLength(500);
        builder.Property(x => x.City).IsRequired().HasMaxLength(100);
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.Property(x => x.LogoUrl).HasMaxLength(500);
        builder.Property(x => x.StoreDescription).HasMaxLength(2000);
        builder.Property(x => x.DefaultCommissionRate).HasPrecision(5, 2);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
