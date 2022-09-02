using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Shared.User.Token;

namespace Shared.User;

public class UserContext<TUser>:DbContext
where TUser:RootUser,new()
{
    public UserContext(DbContextOptions opt):base(opt)
    {
        
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseLazyLoadingProxies();
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RootJwtToken>().HasIndex(x => x.JwtToken).IsUnique();
        modelBuilder.Entity<RootJwtToken>().HasIndex("Device","ApplicationUserId","CurrentlyUsing");
        base.OnModelCreating(modelBuilder);
    }

    public DbSet<RootClaim> Claims { get; set; }
    public DbSet<RootLogin> Logins { get; set; }
    public DbSet<RootRole> Roles { get; set; }
    public DbSet<RootJwtToken> Tokens { get; set; }
    public DbSet<TUser> Users { get; set; }
}