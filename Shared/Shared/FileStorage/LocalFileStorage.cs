using Microsoft.Extensions.Options;
using Shared.FileStorage.Options;

namespace Shared.FileStorage;

public class LocalFileStorage : ILocalFileStorage
{
    public FileStorageType FileStorageType => FileStorageType.Local;
    private string LocalFileRoot { get;}
    private string ApplicationUrl { get; }
    private string FullRoot => Path.Combine(ApplicationUrl, LocalFileRoot);
    public LocalFileStorage(IOptions<LocalFileStorageOption> options)
    {
        LocalFileRoot = options.Value.RootPath ?? "wwwroot";
        ApplicationUrl = options.Value.ApplicationUrl ?? "";
    }
    public async Task<string> UploadFile(Stream fileStream, string fileName, string containerName)
    {
        string fullRootPath = CombinePath(containerName);
        if(!Directory.Exists(fullRootPath))
            Directory.CreateDirectory(fullRootPath);
        fileStream.Seek(0,SeekOrigin.Begin);
        string fullPath = Path.Combine(fullRootPath, fileName);
        await using var file = File.Create(Path.Combine(fullRootPath, fileName));
        await fileStream.CopyToAsync(file);
        return Path.Combine(ApplicationUrl,containerName,fileName);
    }
    public Task<Stream> DownloadFile(string fileName,string containerName)
    {
        string fullDirectoryPath = CombinePath(containerName);
        if(!Directory.Exists(fullDirectoryPath))
            throw new DirectoryNotFoundException($"Directory {fullDirectoryPath} not found");
        if(!File.Exists(Path.Combine(fullDirectoryPath, fileName)))
            throw new FileNotFoundException($"File {fileName} not found");
        return Task.FromResult<Stream>(File.OpenRead(Path.Combine(fullDirectoryPath, fileName)));
    }
    private string CombinePath(string containerName)=>Path.Combine(LocalFileRoot, containerName);
    
}