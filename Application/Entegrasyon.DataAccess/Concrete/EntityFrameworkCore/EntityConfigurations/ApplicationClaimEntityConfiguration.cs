using Entegrasyon.Entity.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class ApplicationClaimEntityConfiguration:IEntityTypeConfiguration<ApplicationClaim>
{
    public void Configure(EntityTypeBuilder<ApplicationClaim> builder)
    {
        builder.ToTable("Claims");
        builder.HasKey(c => c.Id);

        builder.HasIndex(c => c.Name);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(50);
        builder.Property(c => c.Description).HasMaxLength(255);

        builder.HasMany(c => c.Users).WithMany(u => u.Claims)
            .UsingEntity<UsersClaims>();
        builder.HasMany(c => c.Roles).WithMany(r => r.Claims)
            .UsingEntity<RolesClaims>();
    }
}