using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Shared.Utility.Extensions;

namespace Shared.Utility.FileStorage;

public class FileStorageFactory : IFileStorageFactory
{
    private readonly Func<IEnumerable<IFileStorage>> _fileStorageFunc;
    private readonly IConfiguration _configuration;
    private const string FileStorageTypeConfig = "Storage:FileStorageTypeConfig";
    public FileStorageFactory(Func<IEnumerable<IFileStorage>> fileStorageFunc,IConfiguration configuration)
    {
        _fileStorageFunc = fileStorageFunc;
        _configuration = configuration;
    }

    public IFileStorage Create()=> Create(_configuration.GetOrThrow<string>(FileStorageTypeConfig));
    

    public IFileStorage Create(string storageType) =>
        _fileStorageFunc().Single(fs => fs.FileStorageType == GetFileStorageType(storageType));

    private static FileStorageType GetFileStorageType(string storageType)
    { 
        return Enum.TryParse(storageType,true,out FileStorageType fileStorageTypeEnum)
            ? fileStorageTypeEnum
            : FileStorageType.Local;
      
    }
}