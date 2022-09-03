using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Helpers;
using Shared.Security.Jwt;
using Shared.User;

namespace Shared.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddSharedSettings(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IRandomGenerator, RandomGenerator>();
        serviceCollection.AddSingleton<IJwtBlackListService, RedisJwtBlackListService>();
        serviceCollection.AddScoped<ITokenHelper, JwtHelper>();
        return serviceCollection;
    }

    public static IServiceCollection AddDbContextWithUser<TContext,TUser>
        (this IServiceCollection serviceCollection,Action<DbContextOptionsBuilder> options)
        where TContext : UserContext<TUser>
        where TUser:RootUser
    {
        serviceCollection.AddDbContext<TContext>(options);
        serviceCollection.AddDbContext<UserContext<TUser>>(options);
        return serviceCollection;
    }
}