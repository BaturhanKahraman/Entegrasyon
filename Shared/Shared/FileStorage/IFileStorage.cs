using System.IO;
using System.Threading.Tasks;

namespace Shared.FileStorage;

public interface IFileStorage
{
    public FileStorageType FileStorageType { get; }
    Task<string> UploadFile(Stream fileStream, string fileName, string containerName);

    Task<Stream> DownloadFile(string fileName,string containerName);

}