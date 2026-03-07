using Entegrasyon.Entity.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public sealed class RolesClaimsEntityConfiguration : IEntityTypeConfiguration<RolesClaims>
{
    public void Configure(EntityTypeBuilder<RolesClaims> builder)
    {
        builder.ToTable("RolesClaims");

        builder.Property(x => x.Permission)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasKey(x => new { x.RoleId, x.Permission });

        builder.HasOne(x => x.Role)
            .WithMany(r => r.RoleClaims)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
