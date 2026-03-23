using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Categories;
using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Business.Concrete.Auth;
using Entegrasyon.Business.FileStorage;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest;

/// <summary>
/// DI container butunluk testleri — tum registered servisler resolve edilebiliyor mu?
/// Yanlis kayit, eksik dependency veya circular reference varsa burada yakalariz.
/// </summary>
[Trait("Category", "Integration")]
public class DiContainerTests : IntegrationTestBase
{
    public DiContainerTests(PostgreSqlFixture pgFixture) : base(pgFixture) { }

    [Fact]
    public void DbContextFactory_ShouldBeResolvable()
    {
        using var scope = CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        factory.Should().NotBeNull();

        using var context = factory.CreateDbContext();
        context.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(ScopedServiceTypes))]
    public void ScopedService_ShouldBeResolvable(Type serviceType)
    {
        using var scope = CreateScope();
        var service = scope.ServiceProvider.GetRequiredService(serviceType);
        service.Should().NotBeNull($"Service {serviceType.Name} should be resolvable from DI container");
    }

    [Theory]
    [MemberData(nameof(SingletonServiceTypes))]
    public void SingletonService_ShouldBeResolvable(Type serviceType)
    {
        var service = Services.GetRequiredService(serviceType);
        service.Should().NotBeNull($"Singleton {serviceType.Name} should be resolvable from DI container");
    }

    [Fact]
    public void FluentValidator_ShouldBeResolvable()
    {
        using var scope = CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<IFluentValidator>();
        validator.Should().NotBeNull();
    }

    public static TheoryData<Type> ScopedServiceTypes => new()
    {
        // Core business managers
        typeof(IProductService),
        typeof(ICategoryService),
        typeof(IOrderManager),
        typeof(IBrandService),
        typeof(ICustomerManager),
        typeof(IBranchOfficeManager),
        typeof(ICargoCompaniesManager),
        typeof(IOfficeStockManager),
        typeof(IProductVariantManager),
        typeof(IAttributeKeyValueManager),
        typeof(IBarcodeService),
        typeof(IDiscountVoucherManager),
        typeof(ISaleManager),
        typeof(IDashboardManager),
        typeof(IMarketPlaceManager),
        typeof(IProductSyncManager),
        typeof(IDiscountManager),
        typeof(IApplicationLogManager),
        typeof(IApplicationUserManager),
        typeof(IRoleService),
        typeof(IAuthService),
        typeof(INotificationManager),
        typeof(INotificationSettingManager),
        typeof(IReportManager),
        typeof(IApplicationSettingManager),
        typeof(ICategoryAttributeManager),
        typeof(ICategoryAttributeCategoryManager),
        typeof(ICategoryAttributeValueManager),
        typeof(IBrandMatchService),
        typeof(ICategoryMatchService),
        typeof(IImageManager),
        typeof(IMarketplaceOverrideManager),
        typeof(IProductActivityLogger),
        typeof(ILabelService),
        typeof(ILabelTemplateService),
        typeof(ITenantContext),

        // Trendyol
        typeof(ITrendyolApiClient),
        typeof(ITrendyolProductService),
        typeof(ITrendyolStockPriceService),
        typeof(ITrendyolOrderService),
        typeof(ITrendyolProductMapper),
        typeof(ITrendyolCategoryImportService),
        typeof(ITrendyolBrandImporterService),
        typeof(ITrendyolInvoiceService),

        // Hepsiburada
        typeof(IHepsiburadaApiClient),
        typeof(IHepsiburadaProductService),
        typeof(IHepsiburadaListingService),
        typeof(IHepsiburadaOrderService),
        typeof(IHepsiburadaQnAService),
        typeof(IHepsiburadaClaimService),

        // N11
        typeof(IN11SoapClient),
        typeof(IN11ProductService),
        typeof(IN11StockPriceService),
        typeof(IN11OrderService),
        typeof(IN11ClaimService),

        // Pazarama
        typeof(IPazaramaApiClient),
        typeof(IPazaramaProductService),
        typeof(IPazaramaStockPriceService),
        typeof(IPazaramaOrderService),
        typeof(IPazaramaRefundService),
        typeof(IPazaramaBrandService),

        // PttAVM
        typeof(IPttavmCatalogApiClient),
        typeof(IPttavmProductService),
        typeof(IPttavmStockPriceService),
        typeof(IPttavmShipmentApiClient),
        typeof(IPttavmOrderService),
        typeof(IPttavmShippingService),
        typeof(IPttavmInvoiceService),

        // Ciceksepeti
        typeof(ICiceksepetiApiClient),
        typeof(ICiceksepetiCategoryService),
        typeof(ICiceksepetiProductService),
        typeof(ICiceksepetiStockPriceService),
        typeof(ICiceksepetiOrderService),
        typeof(ICiceksepetiInvoiceService),
        typeof(ICiceksepetiReturnService),
        typeof(ICiceksepetiQnAService),

        // E-Fatura
        typeof(ITrendyolEFaturaApiClient),
        typeof(ITrendyolEFaturaService),

        // Validators
        typeof(IFluentValidator),
    };

    public static TheoryData<Type> SingletonServiceTypes => new()
    {
        // Event channels
        typeof(EventChannel<ProductCreatedForMarketplaceEvent>),
        typeof(EventChannel<ProductAddedEvent>),
        typeof(EventChannel<CategoryUpdatedEvent>),
        typeof(EventChannel<CategoryImportRequestedEvent>),
        typeof(EventChannel<CategoryImportCompletedEvent>),
        typeof(EventChannel<NotificationEvent>),
        typeof(EventChannel<ProductUpdatedEvent>),
        typeof(EventChannel<StockPriceChangedEvent>),

        // Storage
        typeof(IMinioFileStorage),
        typeof(IImageProcessingService),
    };
}
