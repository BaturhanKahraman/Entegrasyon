using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Shared.Extensions;

namespace Shared.FileStorage;

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


    public IFileStorage Create(string storageType) =>
        _fileStorageFunc.Single(fs => fs.FileStorageType == GetFileStorageType(storageType));

    private static FileStorageType GetFileStorageType(string storageType)
    {
        return Enum.TryParse(storageType,true,out FileStorageType fileStorageTypeEnum)
            ? fileStorageTypeEnum
            : FileStorage.FileStorageType.Local;

    }
}