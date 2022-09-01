using System.IO;
using System.Threading.Tasks;

namespace Shared.FileStorage;

public class LocalFileStorage : ILocalFileStorage
{
    public FileStorageType FileStorageType => FileStorageType.Local;

    public Task UploadFile(Stream fileStream,string fileName,string containerName)
    {
        throw new NotImplementedException();
    }

    public Task<Stream> DownloadFile(string fileName,string containerName = null)
    {
        throw new NotImplementedException();
    }
}