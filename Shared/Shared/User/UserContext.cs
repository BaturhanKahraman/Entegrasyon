using Microsoft.EntityFrameworkCore;

namespace Shared.User;

public class UserContext<TUser>:DbContext
where TUser:RootUser
{
    protected UserContext(DbContextOptions opt):base(opt)
    {

    }
    public UserContext(DbContextOptions<UserContext<TUser>> opt):base(opt) { }
    

    public DbSet<RootClaim> Claims { get; set; }
    public DbSet<RootLogin> Logins { get; set; }
    public DbSet<RootRole> Roles { get; set; }
    public DbSet<TUser> Users { get; set; }
}