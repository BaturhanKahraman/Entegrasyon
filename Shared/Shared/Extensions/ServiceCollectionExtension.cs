using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Helpers;
using Shared.Security.Jwt;
using Shared.User;
using Shared.User.Services;

namespace Shared.Extensions;

public static class ServiceCollectionExtension
{
    private const string FileStorageType = "Storage:FileStorageType";

    public static IServiceCollection AddSharedSettings(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IRandomGenerator, RandomGenerator>();
        serviceCollection.AddSingleton<IJwtBlackListService, RedisJwtBlackListService>();
        //serviceCollection.AddScoped<ITokenHelper, JwtHelper>();
        return serviceCollection;
    }

    public static IServiceCollection AddUserServices<TUser,TLogin,TRole,TClaim,TContext>(this IServiceCollection services)
    where TUser:RootUser
    where TContext:DbContext
    where TRole:RootRole
    where TClaim:RootClaim
    where TLogin:RootLogin,new()
    {
        services.AddScoped<IUserManager<TUser>, UserManager<TUser, TContext>>();
        services.AddScoped<ILoginManager<TUser>, LoginManager<TUser, TLogin, TContext>>();
        services.AddScoped<IRoleManager<TRole,TClaim>,RoleManager<TRole,TClaim,TContext>>();
        return services;
    }
    

}