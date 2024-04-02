using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Helpers;

namespace Shared.Extensions;

public static class ServiceCollectionExtension
{
    private const string FileStorageType = "Storage:FileStorageType";

    public static IServiceCollection AddSharedSettings(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IRandomGenerator, RandomGenerator>();
        //serviceCollection.AddScoped<ITokenHelper, JwtHelper>();
        return serviceCollection;
    }
}