namespace Entegrasyon.Business.FileStorage;

public interface IMinioFileStorage
{
    Task EnsureBucketExistsAsync();
    Task<string> UploadAsync(Stream stream, string objectName, string contentType);
    Task DeleteAsync(string objectName);
    string GetPublicUrl(string objectName);
}
