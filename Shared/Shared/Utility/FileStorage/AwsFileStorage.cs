using System.IO;
using System.Threading.Tasks;

namespace Shared.Utility.FileStorage;

public class AwsFileStorage : IAwsFileStorage
{
    public FileStorageType FileStorageType => FileStorageType.Aws;

    public Task UploadFile(Stream fileStream,string fileName,string containerName)
    {
        throw new NotImplementedException();
    }

    public Task<Stream> DownloadFile(string fileName,string containerName = null)
    {
        throw new NotImplementedException();
    }
}