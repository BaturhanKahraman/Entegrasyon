namespace Shared.FileStorage;

public interface IMinioFileStorage
{
    Task EnsureBucketExistsAsync();
    Task<string> UploadAsync(Stream stream, string objectName, string contentType);
    string GetPublicUrl(string objectName);
}
