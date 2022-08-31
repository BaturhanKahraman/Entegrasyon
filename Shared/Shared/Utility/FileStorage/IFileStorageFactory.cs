namespace Shared.Utility.FileStorage;

public interface IFileStorageFactory
{
    IFileStorage Create();
    IFileStorage Create(string storageType);
}