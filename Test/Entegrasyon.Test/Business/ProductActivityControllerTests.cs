using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Product.Activity;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Entegrasyon.MVC.Features.Products;
using Entegrasyon.MVC.Features.Products.ViewModels.Activity;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Entegrasyon.UnitTest.Business;

/// <summary>
/// Ürün 360° controller'ının feature-aware davranışı.
/// E-ticaret aktif VE pasif iki senaryo: pasifte pazaryeri/aktivite boş + EcommerceEnabled=false,
/// sipariş/stok her durumda çalışır.
/// </summary>
public class ProductActivityControllerTests
{
    private readonly Mock<IProductActivityPageManager> _pageManager = new();
    private readonly Mock<IProductPerformanceManager> _performanceManager = new();
    private readonly Guid _productId = Guid.NewGuid();

    private ProductActivityController CreateSut() => new(_pageManager.Object, _performanceManager.Object);

    [Fact]
    public async Task Activity_when_ecommerce_disabled_returns_passive_empty_vm()
    {
        _pageManager.Setup(m => m.IsEcommerceEnabledAsync()).ReturnsAsync(false);
        _pageManager
            .Setup(m => m.GetTimelineAsync(_productId, It.IsAny<ProductActivityTimelineFilter>(), It.IsAny<int>(), It.IsAny<bool?>()))
            .ReturnsAsync(new SuccessDataResult<List<ProductActivityLog>>([]));

        var result = await CreateSut().Activity(_productId);

        var vm = result.Should().BeOfType<PartialViewResult>().Subject
            .Model.Should().BeOfType<ProductActivityTimelineVm>().Subject;
        vm.EcommerceEnabled.Should().BeFalse();
        vm.Items.Should().BeEmpty();
        vm.HasMore.Should().BeFalse();
        vm.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task Activity_when_ecommerce_enabled_returns_items_and_computes_pagination()
    {
        const int pageSize = 2;
        var older = DateTimeOffset.UtcNow.AddHours(-2);
        var items = new List<ProductActivityLog>
        {
            new() { Id = 2, ProductId = _productId, Message = "yeni", CreatedAt = DateTimeOffset.UtcNow.AddHours(-1) },
            new() { Id = 1, ProductId = _productId, Message = "eski", CreatedAt = older }
        };
        _pageManager.Setup(m => m.IsEcommerceEnabledAsync()).ReturnsAsync(true);
        _pageManager
            .Setup(m => m.GetTimelineAsync(_productId, It.IsAny<ProductActivityTimelineFilter>(), pageSize, It.IsAny<bool?>()))
            .ReturnsAsync(new SuccessDataResult<List<ProductActivityLog>>(items));

        var result = await CreateSut().Activity(_productId, pageSize: pageSize);

        var vm = result.Should().BeOfType<PartialViewResult>().Subject
            .Model.Should().BeOfType<ProductActivityTimelineVm>().Subject;
        vm.EcommerceEnabled.Should().BeTrue();
        vm.Items.Should().HaveCount(2);
        vm.HasMore.Should().BeTrue("pageSize kadar kayıt geldi → devamı olabilir");
        vm.NextCursor.Should().Be(older, "son (en eski) satırın CreatedAt'i cursor olur");
        vm.NextCursorId.Should().Be(1, "keyset tie-break: son satırın Id'si cursorId olur");
    }

    [Fact]
    public async Task Activity_parses_status_and_marketplace_filter_from_query()
    {
        ProductActivityTimelineFilter? captured = null;
        _pageManager.Setup(m => m.IsEcommerceEnabledAsync()).ReturnsAsync(true);
        _pageManager
            .Setup(m => m.GetTimelineAsync(_productId, It.IsAny<ProductActivityTimelineFilter>(), It.IsAny<int>(), It.IsAny<bool?>()))
            .Callback<Guid, ProductActivityTimelineFilter, int, bool?>((_, f, _, _) => captured = f)
            .ReturnsAsync(new SuccessDataResult<List<ProductActivityLog>>([]));

        await CreateSut().Activity(
            _productId,
            marketplaces: ["Trendyol", "  ", "Trendyol"],
            activityTypes: ["BatchFailed", "bogus"],
            status: "Error");

        captured.Should().NotBeNull();
        captured!.MarketplaceNames.Should().ContainSingle().Which.Should().Be("Trendyol");
        captured.ActivityTypes.Should().ContainSingle().Which.Should().Be(ProductActivityType.BatchFailed);
        captured.Statuses.Should().ContainSingle().Which.Should().Be(ProductActivityStatus.Error);
    }

    [Fact]
    public async Task MarketplaceCards_when_disabled_returns_empty_with_flag_false()
    {
        _pageManager.Setup(m => m.IsEcommerceEnabledAsync()).ReturnsAsync(false);
        _pageManager.Setup(m => m.GetMarketplaceStatusesAsync(_productId, It.IsAny<bool?>()))
            .ReturnsAsync(new SuccessDataResult<List<ProductMarketplaceStatusDto>>([]));

        var result = await CreateSut().MarketplaceCards(_productId);

        var vm = result.Should().BeOfType<PartialViewResult>().Subject
            .Model.Should().BeOfType<ProductMarketplaceCardsVm>().Subject;
        vm.EcommerceEnabled.Should().BeFalse();
        vm.Cards.Should().BeEmpty();
    }

    [Fact]
    public async Task Orders_returns_data_regardless_of_ecommerce_flag()
    {
        var orders = new List<ProductOrderReferenceDto>
        {
            new(Guid.NewGuid(), "POS-1", DateTimeOffset.UtcNow, "Mağaza", "Kırmızı / 42", 2)
        };
        _pageManager.Setup(m => m.GetOrdersAsync(_productId, It.IsAny<int>()))
            .ReturnsAsync(new SuccessDataResult<List<ProductOrderReferenceDto>>(orders));

        var result = await CreateSut().Orders(_productId);

        result.Should().BeOfType<PartialViewResult>().Subject
            .Model.Should().BeAssignableTo<IReadOnlyList<ProductOrderReferenceDto>>()
            .Which.Should().HaveCount(1);
        _pageManager.Verify(m => m.IsEcommerceEnabledAsync(), Times.Never,
            "siparişler fiziksel mağaza için de gösterilir; feature-gate uygulanmaz");
    }

    [Fact]
    public async Task StockMovements_returns_data_regardless_of_ecommerce_flag()
    {
        var movements = new List<ProductStockMovementDto>
        {
            new(Guid.NewGuid(), "Mavi / 38", StockMovementType.Sale, -1, 45, 44, "Sale", "N11-1", null, DateTimeOffset.UtcNow)
        };
        _pageManager.Setup(m => m.GetStockMovementsAsync(_productId, It.IsAny<int>()))
            .ReturnsAsync(new SuccessDataResult<List<ProductStockMovementDto>>(movements));

        var result = await CreateSut().StockMovements(_productId);

        result.Should().BeOfType<PartialViewResult>().Subject
            .Model.Should().BeAssignableTo<IReadOnlyList<ProductStockMovementDto>>()
            .Which.Should().HaveCount(1);
        _pageManager.Verify(m => m.IsEcommerceEnabledAsync(), Times.Never);
    }
}
