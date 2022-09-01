using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Shared.Extensions;

public static class ConfigurationExtension
{
    public static T GetOrThrow<T>(this IConfiguration configuration,string key)
    {
        var value = configuration[key];
        if(value == null)
        {
            throw new KeyNotFoundException($"{key} is not found in configuration");
        }
        return configuration.GetValue<T>(key);
    }
}