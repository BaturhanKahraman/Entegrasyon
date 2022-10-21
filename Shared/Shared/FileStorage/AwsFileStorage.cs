using Amazon.Runtime;
using Amazon.S3.Transfer;
using Amazon.S3;
using Microsoft.Extensions.Options;
using System.IO;
using System.Threading.Tasks;
using Shared.FileStorage.Options;

namespace Shared.FileStorage;

public class AwsFileStorage : IAwsFileStorage
{
    public FileStorageType FileStorageType => FileStorageType.Aws;

    private static IAmazonS3 _client;
    
    public AwsFileStorage(IOptions<AwsFileStorageOption> option)
    {
        if(option.Value.AwsAccessKey is null || option.Value.AwsSecretKey is null)
            return;
        AWSCredentials credentials = new BasicAWSCredentials(option.Value.AwsAccessKey,option.Value.AwsSecretKey);
            _client = new AmazonS3Client(credentials);
    }
    public async Task<Stream> DownloadFile(string fileName,string container)
    {
        TransferUtility transferUtility = new TransferUtility(_client);
        TransferUtilityOpenStreamRequest transferUtilityDownloadRequest = new TransferUtilityOpenStreamRequest
        {
            BucketName = container,
            Key = fileName
        };
        return await transferUtility.OpenStreamAsync(transferUtilityDownloadRequest).ConfigureAwait(false);
    }

    public async Task<string> UploadFile(Stream fileStream, string fileName, string container)
    {
        using TransferUtility utility = new TransferUtility(_client);
        TransferUtilityUploadRequest request = new TransferUtilityUploadRequest
        {
            BucketName = container,
            Key = fileName,
            InputStream = fileStream
        };
        await utility.UploadAsync(request).ConfigureAwait(false);

        return Path.Combine(container,fileName);
    }

    public IEnumerable<string> GetFiles(string container)
    {
        throw new System.NotImplementedException();
    }

    public async Task DeleteFile(string fileName,string container)
    {
        throw new System.NotImplementedException();
    }
}