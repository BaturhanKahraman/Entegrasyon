using Entegrasyon.Entity.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public sealed class UsersClaimsEntityConfiguration : IEntityTypeConfiguration<UsersClaims>
{
    public void Configure(EntityTypeBuilder<UsersClaims> builder)
    {
        builder.ToTable("UsersClaims");

        builder.Property(x => x.Permission)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasKey(x => new { x.ApplicationUserId, x.Permission });

        builder.HasOne(x => x.ApplicationUser)
            .WithMany(u => u.MyProperty)
            .HasForeignKey(x => x.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
