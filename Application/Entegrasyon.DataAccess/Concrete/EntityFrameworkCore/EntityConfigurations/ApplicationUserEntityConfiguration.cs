
using Entegrasyon.Entity.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class ApplicationUserEntityConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.NormalizedUserName);
        builder.Property(x => x.Email).HasMaxLength(100);
        builder.Property(x => x.NormalizedEmail).HasMaxLength(100);
        builder.Property(x => x.Name).HasMaxLength(60);
        builder.Property(x => x.Surname).HasMaxLength(60);
        builder.Property(x => x.FullName).HasMaxLength(120);
        builder.Property(x => x.UserName).HasMaxLength(30);
        builder.Property(x => x.NormalizedUserName).HasMaxLength(30);

        builder.Property(u => u.RowVersion)
            .IsConcurrencyToken()
            .ValueGeneratedOnAddOrUpdate();

        builder.HasOne(u => u.DefaultBranchOffice).WithMany(bo => bo.Users).IsRequired(false).OnDelete(DeleteBehavior.SetNull);

        // Remember-me için persist edilen son seçili ofis
        builder.HasOne(u => u.LastSelectedBranchOffice)
            .WithMany()
            .HasForeignKey(u => u.LastSelectedBranchOfficeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Logins).WithOne(x => x.User);
        builder.HasMany(u => u.Roles).WithMany(r => r.Users).UsingEntity<UsersRoles>();

        // UserBranchOffice junction navigation — detaylı config UserBranchOfficeEntityConfiguration'da
        builder.HasMany(u => u.UserBranchOffices)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId);
    }
}
