using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Constants;
using Entegrasyon.Business.Tenants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Product.Activity;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.UnitTest.Business;

/// <summary>
/// Ürün 360° veri orkestratörünün feature-gating davranışı.
/// KRİTİK: E-ticaret/pazaryeri opsiyonel — kapalıyken pazaryeri/aktivite sorgusu HİÇ çalışmamalı
/// (DB'ye gidilmemeli, logger çağrılmamalı) ki sadece fiziksel mağaza kullanan esnafta NRE/boş-liste
/// hatası olmasın.
/// </summary>
public class ProductActivityPageManagerTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _contextFactory = new();
    private readonly Mock<IProductActivityLogger> _activityLogger = new();
    private readonly Mock<IFeatureService> _featureService = new();
    private readonly Mock<IApplicationLogManager> _appLog = new();
    private readonly Mock<ILogger<ProductActivityPageManager>> _logger = new();

    private ProductActivityPageManager CreateSut() => new(
        _contextFactory.Object,
        _activityLogger.Object,
        _featureService.Object,
        _appLog.Object,
        _logger.Object);

    private void SetEcommerce(bool enabled) =>
        _featureService.Setup(f => f.IsFeatureEnabledAsync(PermissionConstants.MarketplaceView))
            .ReturnsAsync(enabled);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsEcommerceEnabledAsync_returns_feature_service_value(bool enabled)
    {
        SetEcommerce(enabled);

        var result = await CreateSut().IsEcommerceEnabledAsync();

        result.Should().Be(enabled);
    }

    [Fact]
    public async Task GetMarketplaceStatusesAsync_when_ecommerce_disabled_returns_empty_without_touching_db()
    {
        SetEcommerce(false);

        var result = await CreateSut().GetMarketplaceStatusesAsync(Guid.NewGuid());

        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
        _contextFactory.Verify(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Never,
            "e-ticaret kapalıyken pazaryeri sorgusu DB'ye gitmemeli");
    }

    [Fact]
    public async Task GetTimelineAsync_when_ecommerce_disabled_returns_empty_without_calling_logger()
    {
        SetEcommerce(false);

        var result = await CreateSut().GetTimelineAsync(Guid.NewGuid(), ProductActivityTimelineFilter.Empty);

        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
        _activityLogger.Verify(
            l => l.GetTimelineAsync(It.IsAny<Guid>(), It.IsAny<ProductActivityTimelineFilter>(), It.IsAny<int>()),
            Times.Never,
            "e-ticaret kapalıyken aktivite timeline sorgusu çalışmamalı");
    }

    [Fact]
    public async Task GetTimelineAsync_when_flag_passed_does_not_reconsult_feature_service()
    {
        // M3: controller feature'ı bir kez çözer ve aşağı geçer → manager TEKRAR sormaz (tek kaynak).
        _activityLogger
            .Setup(l => l.GetTimelineAsync(It.IsAny<Guid>(), It.IsAny<ProductActivityTimelineFilter>(), It.IsAny<int>()))
            .ReturnsAsync(new SuccessDataResult<List<ProductActivityLog>>([]));

        await CreateSut().GetTimelineAsync(Guid.NewGuid(), ProductActivityTimelineFilter.Empty, 20, ecommerceEnabled: true);

        _featureService.Verify(f => f.IsFeatureEnabledAsync(It.IsAny<string>()), Times.Never,
            "flag dışarıdan verildiyse manager feature servisini yeniden çağırmamalı");
    }

    [Fact]
    public async Task GetTimelineAsync_when_flag_passed_false_short_circuits_without_logger()
    {
        await CreateSut().GetTimelineAsync(Guid.NewGuid(), ProductActivityTimelineFilter.Empty, 20, ecommerceEnabled: false);

        _activityLogger.Verify(
            l => l.GetTimelineAsync(It.IsAny<Guid>(), It.IsAny<ProductActivityTimelineFilter>(), It.IsAny<int>()),
            Times.Never);
        _featureService.Verify(f => f.IsFeatureEnabledAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetTimelineAsync_when_logger_throws_degrades_gracefully_to_empty_error()
    {
        SetEcommerce(true);
        _activityLogger
            .Setup(l => l.GetTimelineAsync(It.IsAny<Guid>(), It.IsAny<ProductActivityTimelineFilter>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var result = await CreateSut().GetTimelineAsync(Guid.NewGuid(), ProductActivityTimelineFilter.Empty);

        result.Success.Should().BeFalse("timeline sorgusu patlasa bile diğer sekmelerle tutarlı graceful degrade");
        result.Data.Should().BeEmpty();
        _appLog.Verify(a => a.AddLog("Aktivite geçmişi yüklenemedi.", LogType.Product, LogAction.List,
            It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTimelineAsync_when_ecommerce_enabled_delegates_to_activity_logger()
    {
        SetEcommerce(true);
        var productId = Guid.NewGuid();
        var filter = new ProductActivityTimelineFilter(
            MarketplaceNames: new[] { "Trendyol" },
            Statuses: new[] { ProductActivityStatus.Error });
        var expected = new List<ProductActivityLog>
        {
            new() { Id = 1, ProductId = productId, Message = "test", ActivityType = ProductActivityType.BatchFailed }
        };
        _activityLogger
            .Setup(l => l.GetTimelineAsync(productId, filter, 20))
            .ReturnsAsync(new SuccessDataResult<List<ProductActivityLog>>(expected));

        var result = await CreateSut().GetTimelineAsync(productId, filter, 20);

        result.Success.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(expected);
        _activityLogger.Verify(l => l.GetTimelineAsync(productId, filter, 20), Times.Once);
    }
}
