using Castle.Core.Configuration;
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
    
}