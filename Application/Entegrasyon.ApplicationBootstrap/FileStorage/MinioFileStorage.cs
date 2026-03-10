using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

using Entegrasyon.Business.FileStorage;

namespace Entegrasyon.ApplicationBootstrap.FileStorage;

public class MinioFileStorage : IMinioFileStorage
{
    private readonly IMinioClient _client;
    private readonly string _bucketName;
    private readonly string _publicBaseUrl;

    public MinioFileStorage(IOptions<MinioOptions> options)
    {
        var opt = options.Value;
        _client = new MinioClient()
            .WithEndpoint(opt.Endpoint)
            .WithCredentials(opt.AccessKey, opt.SecretKey)
            .WithSSL(opt.UseSSL)
            .Build();
        _bucketName = opt.BucketName;
        _publicBaseUrl = opt.PublicBaseUrl.TrimEnd('/');
    }

    public async Task EnsureBucketExistsAsync()
    {
        bool exists = await _client.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_bucketName));

        if (!exists)
        {
            await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));

            // Set public-read policy so images are served directly without auth
            string policy = $$"""
                {
                  "Version": "2012-10-17",
                  "Statement": [{
                    "Effect": "Allow",
                    "Principal": { "AWS": ["*"] },
                    "Action": ["s3:GetObject"],
                    "Resource": ["arn:aws:s3:::{{_bucketName}}/*"]
                  }]
                }
                """;
            await _client.SetPolicyAsync(
                new SetPolicyArgs().WithBucket(_bucketName).WithPolicy(policy));
        }
    }

    public async Task<string> UploadAsync(Stream stream, string objectName, string contentType)
    {
        stream.Seek(0, SeekOrigin.Begin);
        await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType(contentType));

        return GetPublicUrl(objectName);
    }

    public string GetPublicUrl(string objectName)
        => $"{_publicBaseUrl}/{_bucketName}/{objectName}";
}
