using Entegrasyon.Business.FileStorage;
using Entegrasyon.DataAccess;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.MVC.Test;

public class MvcTestFactory : WebApplicationFactory<Program>
{
    private const string TestConnectionString =
        "Host=192.168.1.78;Port=5432;Database=IntegrationDb;Username=baturhan;Password=DiHRrP6dY8nC*M;Include Error Detail=true";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Main"] = TestConnectionString,
                ["ConnectionStrings:AdminPanel"] = "Host=192.168.1.78;Port=5432;Database=AdminPanelDb;Username=baturhan;Password=DiHRrP6dY8nC*M",
                ["ConnectionStrings:Redis"] = "192.168.1.78:6379",
                ["Minio:Endpoint"] = "localhost:9000",
                ["Minio:AccessKey"] = "test",
                ["Minio:SecretKey"] = "test1234",
                ["Minio:BucketName"] = "test",
                ["Minio:PublicBaseUrl"] = "http://localhost:9000",
                ["Tenant:DefaultSubdomain"] = "dev"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove all hosted/background services to prevent polling during tests
            services.RemoveAll<IHostedService>();

            // Replace MinIO with a no-op implementation for tests
            services.RemoveAll<IMinioFileStorage>();
            services.AddSingleton<IMinioFileStorage, NoOpMinioFileStorage>();

            // Replace IImageProcessingService with a no-op
            services.RemoveAll<IImageProcessingService>();
            services.AddSingleton<IImageProcessingService, NoOpImageProcessingService>();

            // Replace DbContext factory — the original captures connection string at
            // registration time before ConfigureAppConfiguration runs, so we must override it
            services.RemoveAll<IDbContextFactory<IntegrationDbContext>>();
            services.AddScoped<IDbContextFactory<IntegrationDbContext>>(sp =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                return new TenantDbContextFactory(() => TestConnectionString, loggerFactory);
            });
        });
    }
}

internal class NoOpMinioFileStorage : IMinioFileStorage
{
    public Task EnsureBucketExistsAsync() => Task.CompletedTask;

    public Task<string> UploadAsync(Stream stream, string objectName, string contentType)
        => Task.FromResult($"http://localhost:9000/test/{objectName}");

    public string GetPublicUrl(string objectName)
        => $"http://localhost:9000/test/{objectName}";
}

internal class NoOpImageProcessingService : IImageProcessingService
{
    public Task<ImageUploadResult> ProcessAndUploadAsync(Stream imageStream, string productId, string variantId)
        => Task.FromResult(new ImageUploadResult(
            BaseKey: $"{productId}/{variantId}/{Guid.NewGuid()}",
            OriginalUrl: $"http://localhost:9000/test/{productId}/{variantId}/original.webp",
            MediumUrl: $"http://localhost:9000/test/{productId}/{variantId}/medium.webp",
            ThumbUrl: $"http://localhost:9000/test/{productId}/{variantId}/thumb.webp",
            Width: 800,
            Height: 600,
            FileSizeBytes: 1024));
}
