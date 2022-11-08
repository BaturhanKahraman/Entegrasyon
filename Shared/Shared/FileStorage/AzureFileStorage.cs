namespace Shared.FileStorage;

public class AzureFileStorage : IAzureFileStorage
{
    public FileStorageType FileStorageType => FileStorageType.Azure;

    public Task<string> UploadFile(Stream fileStream, string fileName, string containerName)
    {
        return Task.FromResult(Path.Combine(containerName,fileName));
        
    }

    public Task<Stream> DownloadFile(string fileName,string containerName = null)
    {
        throw new NotImplementedException();
    }
}