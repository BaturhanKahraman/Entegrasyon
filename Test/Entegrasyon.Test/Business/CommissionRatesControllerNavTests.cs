using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Results;
using Entegrasyon.MVC.Features.MarketplaceSync;
using Entegrasyon.MVC.Infrastructure.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace Entegrasyon.UnitTest.Business;

/// <summary>
/// Sol menü (sidebar) nav-active doğruluğu: "Komisyon Oranlari"
/// (/marketplace/commission-rates) sidebar'da kendi öğesidir ("commission-rates").
/// Sayfa KENDİ menü öğesini aktif göstermeli; "Senkronizasyon"u ("marketplace-sync") değil.
/// </summary>
public class CommissionRatesControllerNavTests
{
    private readonly Mock<ICommissionCalculator> _commissionCalculator = new();
    private readonly Mock<IMarketPlaceManager> _marketPlaceManager = new();

    private CommissionRatesController CreateSut()
    {
        var controller = new CommissionRatesController(
            _commissionCalculator.Object,
            _marketPlaceManager.Object);

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
    public async Task Index_marks_commission_rates_nav_active()
    {
        _marketPlaceManager.Setup(m => m.GetAllAsync())
            .ReturnsAsync(new SuccessDataResult<List<MarketPlace>>([]));
        _commissionCalculator
            .Setup(m => m.GetCommissionRatesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessDataResult<List<MarketplaceCommissionRateDto>>([]));

        var sut = CreateSut();

        await sut.Index();

        sut.ViewData.GetActiveNav().Should().Be("commission-rates",
            "Komisyon Oranlari sayfası kendi menü öğesini aktif göstermeli, Senkronizasyon'u değil");
    }
}
