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

    public AwsFileStorage()
    {

    }
    public AwsFileStorage(IOptions<AwsFileStorageOption>? option = null)
    {
        if(option!.Value == null)
            return;
        AWSCredentials credentials = new BasicAWSCredentials(option.Value.AWSAccessKey,option.Value.AWSSecretKey);
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

    public async Task UploadFile(Stream fileStream,string fileName,string container)
    {
        TransferUtility utility = new TransferUtility(_client);
        TransferUtilityUploadRequest request = new TransferUtilityUploadRequest
        {
            BucketName = container,
            Key = fileName,
            InputStream = fileStream
        };
        await utility.UploadAsync(request).ConfigureAwait(false);
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