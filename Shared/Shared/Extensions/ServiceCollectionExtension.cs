using Microsoft.Extensions.DependencyInjection;
using Shared.Helpers;
using Shared.Security.Jwt;
using Shared.User;

namespace Shared.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddSharedSettings(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IRandomHelper, RandomHelper>();
        serviceCollection.AddSingleton<IJwtBlackListService, RedisJwtBlackListService>();
        return serviceCollection;
    }
}