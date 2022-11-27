using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Extensions;
using Shared.FileStorage.Options;

namespace Shared.FileStorage;

public static class FileStorageExtension
{
    public static IServiceCollection AddLocalFileStorage(this IServiceCollection serviceCollection,IConfiguration config)
    {
        serviceCollection.Configure<LocalFileStorageOption>(config);
        serviceCollection.AddSingleton(x =>
            x.GetService<FileStorageFactory>()!.Create(FileStorageType.Local));
        return serviceCollection;
    }
    public static IServiceCollection AddAwsFileStorage(this IServiceCollection serviceCollection,Action<AwsFileStorageOption> option)
    {
        serviceCollection.Configure(option);
        serviceCollection.AddSingleton(x =>
            x.GetService<FileStorageFactory>()!.Create(FileStorageType.Aws));
        return serviceCollection;
    }
    public static IServiceCollection AddFileStorageCore(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IAwsFileStorage,AwsFileStorage>();
        serviceCollection.AddSingleton<IAzureFileStorage,AzureFileStorage>();
        serviceCollection.AddSingleton<ILocalFileStorage,LocalFileStorage>();
        serviceCollection.AddSingleton<FileStorageFactory>();
        serviceCollection.AddSingleton<IEnumerable<IFileStorage>>(x => new List<IFileStorage>
        {
            x.GetService<IAwsFileStorage>()!,
            x.GetService<IAzureFileStorage>()!,
            x.GetService<ILocalFileStorage>()!
        });
        return serviceCollection;
    }
}
public class FileStorageFactory : IFileStorageFactory
{
    private readonly IEnumerable<IFileStorage> _fileStorageFunc;
    private readonly IConfiguration _configuration;
    private const string FileStorageType = "Storage:FileStorageType";
    public FileStorageFactory(IEnumerable<IFileStorage> fileStorageFunc,IConfiguration configuration)
    {
        _fileStorageFunc = fileStorageFunc;
        _configuration = configuration;
    }

    public IFileStorage Create() => Create(_configuration.GetOrThrow<string>(FileStorageType));

    public IFileStorage Create(FileStorageType type) =>
        _fileStorageFunc.Single(fs => fs.FileStorageType == type);
    public IFileStorage Create(string storageType) =>
        _fileStorageFunc.Single(fs => fs.FileStorageType == GetFileStorageType(storageType));

    private static FileStorageType GetFileStorageType(string storageType)
    {
        return Enum.TryParse(storageType,true,out FileStorageType fileStorageTypeEnum)
            ? fileStorageTypeEnum
            : FileStorage.FileStorageType.Local;

    }
}