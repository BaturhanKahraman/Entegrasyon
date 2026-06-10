using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Results;
using Entegrasyon.MVC.Features.MarketplaceSync;
using Entegrasyon.MVC.Infrastructure.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace Entegrasyon.UnitTest.Business;

/// <summary>
/// Sol menü (sidebar) nav-active doğruluğu: "Senkronizasyon" (/marketplace/sync) ve
/// "Ürün Eşleştirme" (/marketplace/matching) ayrı menü öğeleridir. Eşleştirme sayfası
/// KENDİ menü öğesini ("marketplace-matching") aktif göstermeli; "Senkronizasyon"u değil.
/// </summary>
public class ProductSyncControllerNavTests
{
    private readonly Mock<IMarketPlaceManager> _marketPlaceManager = new();
    private readonly Mock<IProductSyncManager> _productSyncManager = new();
    private readonly Mock<IProductActivityLogger> _activityLogger = new();

    private ProductSyncController CreateSut()
    {
        var controller = new ProductSyncController(
            _marketPlaceManager.Object,
            _productSyncManager.Object,
            _activityLogger.Object);

        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        controller.ViewData = new ViewDataDictionary(
            new EmptyModelMetadataProvider(), new ModelStateDictionary());
        controller.TempData = new TempDataDictionary(
            httpContext, Mock.Of<ITempDataProvider>());

        return controller;
    }

    [Fact]
    public async Task Index_marks_marketplace_matching_nav_active()
    {
        _marketPlaceManager.Setup(m => m.GetAllAsync())
            .ReturnsAsync(new SuccessDataResult<List<MarketPlace>>([]));
        _productSyncManager.Setup(m => m.GetSyncSummaryAsync(It.IsAny<int>()))
            .ReturnsAsync(new ProductSyncSummaryDto(0, 0, 0, 0, 0));
        _productSyncManager
            .Setup(m => m.GetProductSyncListAsync(
                It.IsAny<int>(), It.IsAny<MarketplaceSyncState?>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new SuccessDataResult<Pageable<ProductSyncListItemDto>>(
                new Pageable<ProductSyncListItemDto>([], 0, 20, 0)));

        var sut = CreateSut();

        await sut.Index();

        sut.ViewData.GetActiveNav().Should().Be("marketplace-matching",
            "Ürün Eşleştirme sayfası kendi menü öğesini aktif göstermeli, Senkronizasyon'u değil");
    }
}
