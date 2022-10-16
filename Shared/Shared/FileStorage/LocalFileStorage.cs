using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Shared.FileStorage.Options;

namespace Shared.FileStorage;

public class LocalFileStorage : ILocalFileStorage
{
    public FileStorageType FileStorageType => FileStorageType.Local;
    public string LocalFileRoot { get;}
    public LocalFileStorage(IOptions<LocalFileStorageOption> options)
    {
        LocalFileRoot = options.Value.RootPath;
    }
    public async Task UploadFile(Stream fileStream,string fileName,string containerName)
    {
        if(!Directory.Exists(CombinePath(containerName)))
            Directory.CreateDirectory(CombinePath(containerName!));
        fileStream.Seek(0,SeekOrigin.Begin);
        await using var file = File.Create(Path.Combine(CombinePath(containerName),fileName));
        await fileStream.CopyToAsync(file);
    }
    public Task<Stream> DownloadFile(string fileName,string containerName)
    {
        if(!Directory.Exists(CombinePath(containerName)))
            throw new DirectoryNotFoundException($"Directory {CombinePath(containerName)} not found");
        if(!File.Exists(Path.Combine(CombinePath(containerName),fileName)))
            throw new FileNotFoundException($"File {fileName} not found");
        return Task.FromResult<Stream>(File.OpenRead(Path.Combine(CombinePath(containerName),fileName)));
    }
    private string CombinePath(string containerName)=>Path.Combine(LocalFileRoot, containerName);
    
}