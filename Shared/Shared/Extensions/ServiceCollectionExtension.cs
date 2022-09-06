using Castle.Core.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Helpers;
using Shared.Security.Jwt;
using Shared.User;
using Shared.User.Services;

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

    public static IServiceCollection AddUserServices<TUser,TLogin,TContext>(this IServiceCollection services)
    where TUser:RootUser
    where TContext:DbContext
    where TLogin:RootLogin,new()
    {
        services.AddScoped<IUserManager<TUser>, UserManager<TUser, TContext>>();
        services.AddScoped<ILoginManager<TUser>, LoginManager<TUser, TLogin, TContext>>();
        
        return services;
    }
    
}