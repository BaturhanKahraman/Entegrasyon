using Entegrasyon.Business.FileStorage;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Entegrasyon.IntegrationTest.Fixtures;

/// <summary>
/// WebApplicationFactory override — gercek DI container + Testcontainers PostgreSQL.
/// Background servisler devre disi birakilir, MinIO/ImageProcessing mock'lanir.
/// </summary>
public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public IntegrationTestWebAppFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTest");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("appsettings.IntegrationTest.json", optional: true);
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Main"] = _connectionString,
                ["ConnectionStrings:Redis"] = "localhost:6379"
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

            // Replace DbContext factory to use Testcontainers connection string
            services.RemoveAll<IDbContextFactory<IntegrationDbContext>>();
            services.RemoveAll<DbContextOptions<IntegrationDbContext>>();

            services.AddDbContextFactory<IntegrationDbContext>(options =>
            {
                options.UseNpgsql(_connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                options.EnableDetailedErrors();
            });
        });
    }
}

/// <summary>
/// No-op MinIO implementation for integration tests.
/// </summary>
internal class NoOpMinioFileStorage : IMinioFileStorage
{
    public Task EnsureBucketExistsAsync() => Task.CompletedTask;

    public Task<string> UploadAsync(Stream stream, string objectName, string contentType)
        => Task.FromResult($"http://localhost:9000/test/{objectName}");

    public string GetPublicUrl(string objectName)
        => $"http://localhost:9000/test/{objectName}";
}

/// <summary>
/// No-op ImageProcessing implementation for integration tests.
/// </summary>
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
