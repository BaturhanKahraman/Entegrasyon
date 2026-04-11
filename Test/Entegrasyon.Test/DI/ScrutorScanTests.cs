using Entegrasyon.ApplicationBootstrap;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Notifications;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;

namespace Entegrasyon.UnitTest.DI;

/// <summary>
/// Safety-net: verifies that all standard-convention services resolve after
/// the Scrutor scan block is in place. Run RED before Task 2, GREEN after.
/// </summary>
public class ScrutorScanTests
{
    private static IServiceProvider BuildProvider()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Force all UseMock=true so real HTTP clients are never created
                // (Trendyol + Hepsiburada + N11 + Pazarama + Amazon + Pttavm Faz 3.1-3.6'da kaldirildi — digerleri Faz 3.7+)
                ["Ciceksepeti:UseMock"] = "true",
                ["Temu:UseMock"] = "true",
                ["YurticiKargo:UseMock"] = "true",
                ["SuratKargo:UseMock"] = "true",
                ["ArasKargo:UseMock"] = "true",
                ["TrendyolEFatura:UseMock"] = "true",
                ["EInvoice:UseMock"] = "true",
                ["ConnectionStrings:Main"] = "Host=localhost;Database=test;",
                ["Minio:Endpoint"] = "localhost:9000",
                ["Minio:AccessKey"] = "test",
                ["Minio:SecretKey"] = "test",
            })
            .Build();

        var services = new ServiceCollection();

        // Minimal ASP.NET Core services needed by the DI registrations
        services.AddLogging();
        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddSignalR();

        // Register a stub DbContext so the factory lambda resolves
        services.AddDbContext<IntegrationDbContext>(opts =>
            opts.UseInMemoryDatabase("ScrutorTest"), ServiceLifetime.Scoped);
        services.AddDbContextFactory<IntegrationDbContext>(opts =>
            opts.UseInMemoryDatabase("ScrutorTest"), ServiceLifetime.Scoped);

        // HybridCache — business managers inject this
        services.AddHybridCache();

        // The method under test
        services.AddApplicationDependencies(config);
        services.AddNotification();
        services.AddStorefrontServices();
        services.AddStorageServices(config);

        return services.BuildServiceProvider();
    }

    // --- Core business services (standard IXxx → Xxx convention) ---

    [Fact] public void IBranchOfficeManager_Resolves() =>
        BuildProvider().GetRequiredService<IBranchOfficeManager>().Should().NotBeNull();

    [Fact] public void ICustomerManager_Resolves() =>
        BuildProvider().GetRequiredService<ICustomerManager>().Should().NotBeNull();

    [Fact] public void IProductVariantManager_Resolves() =>
        BuildProvider().GetRequiredService<IProductVariantManager>().Should().NotBeNull();

    [Fact] public void IOfficeStockManager_Resolves() =>
        BuildProvider().GetRequiredService<IOfficeStockManager>().Should().NotBeNull();

    [Fact] public void IAttributeKeyValueManager_Resolves() =>
        BuildProvider().GetRequiredService<IAttributeKeyValueManager>().Should().NotBeNull();

    [Fact] public void ICategoryAttributeManager_Resolves() =>
        BuildProvider().GetRequiredService<ICategoryAttributeManager>().Should().NotBeNull();

    [Fact] public void ICategoryMatchService_Resolves() =>
        BuildProvider().GetRequiredService<ICategoryMatchService>().Should().NotBeNull();

    [Fact] public void IBrandMatchService_Resolves() =>
        BuildProvider().GetRequiredService<IBrandMatchService>().Should().NotBeNull();

    [Fact] public void INotificationManager_Resolves() =>
        BuildProvider().GetRequiredService<INotificationManager>().Should().NotBeNull();

    [Fact] public void IChatManager_Resolves() =>
        BuildProvider().GetRequiredService<IChatManager>().Should().NotBeNull();

    [Fact] public void IReportManager_Resolves() =>
        BuildProvider().GetRequiredService<IReportManager>().Should().NotBeNull();

    [Fact] public void IDashboardManager_Resolves() =>
        BuildProvider().GetRequiredService<IDashboardManager>().Should().NotBeNull();

    [Fact] public void IOrderManager_Resolves() =>
        BuildProvider().GetRequiredService<IOrderManager>().Should().NotBeNull();

    [Fact] public void IBulkOperationManager_Resolves() =>
        BuildProvider().GetRequiredService<IBulkOperationManager>().Should().NotBeNull();

    [Fact] public void IEInvoiceManager_Resolves() =>
        BuildProvider().GetRequiredService<IEInvoiceManager>().Should().NotBeNull();

    // --- Naming-exception services (must resolve via explicit override) ---

    [Fact] public void ICategoryService_Resolves_As_CategoryManager() =>
        BuildProvider().GetRequiredService<ICategoryService>().Should().BeOfType<CategoryManager>();

    [Fact] public void IProductService_Resolves_As_ProductManager() =>
        BuildProvider().GetRequiredService<IProductService>().Should().BeOfType<ProductManager>();

    [Fact] public void ILabelService_Resolves_As_LabelManager() =>
        BuildProvider().GetRequiredService<ILabelService>().Should().BeOfType<LabelManager>();

    [Fact] public void ILabelTemplateService_Resolves_As_LabelTemplateManager() =>
        BuildProvider().GetRequiredService<ILabelTemplateService>().Should().BeOfType<LabelTemplateManager>();

    [Fact] public void ITenantContext_Resolves() =>
        BuildProvider().GetRequiredService<ITenantContext>().Should().NotBeNull();

    // --- Storefront services ---

    [Fact] public void IStorefrontSettingsManager_Resolves() =>
        BuildProvider().GetRequiredService<IStorefrontSettingsManager>().Should().NotBeNull();

    [Fact] public void ICartManager_Resolves() =>
        BuildProvider().GetRequiredService<ICartManager>().Should().NotBeNull();

    [Fact] public void ICheckoutManager_Resolves() =>
        BuildProvider().GetRequiredService<ICheckoutManager>().Should().NotBeNull();

    // --- Mock-mode marketplace services ---
    //
    // NOT: Trendyol mock servisi Faz 3.1'de kaldirildi. Gercek TrendyolProductService
    // HTTP client dependency'leri (ITrendyolApiClient vs.) nedeniyle bu mini DI
    // container'da resolve edilemez — integration test kapsaminda (WireMock ile)
    // dogrulanmali. ITrendyolProductService_Resolves_Mock testi bu yuzden silindi.
    // Hepsiburada, Amazon (ve digerleri) Faz 3.2+ sirasinda ayni sekilde silinecek.

}
