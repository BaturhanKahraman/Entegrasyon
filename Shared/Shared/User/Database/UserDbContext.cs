using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.User.Database;
public static class UserDbContextExtensions
{
    public static IServiceCollection AddUserDbContext<TUser>(this IServiceCollection serviceDescriptors,Action<DbContextOptionsBuilder> options=null)
        where TUser : RootUser, new()
    {
        serviceDescriptors.AddDbContext<UserDbContext<TUser>>(options);
        return serviceDescriptors;
    }
}
public class UserDbContext<T>:DbContext
    where T : RootUser, new()
{

    public UserDbContext(DbContextOptions context):base(context)
    {
        
    }

    public DbSet<RootClaim> Claims { get; set; }
    public DbSet<RootLogin> Logins { get; set; }
    public DbSet<RootRole> Roles { get; set; }
    public DbSet<T> Users { get; set; }



}
