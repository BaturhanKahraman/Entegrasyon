using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Receipts;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Receipts;
using Entegrasyon.Entity.Results;
using Moq;

namespace Entegrasyon.UnitTest.Business;

public class ReceiptRendererTests
{
    private readonly Mock<IReceiptTemplateManager> _mgr = new();

    private static SaleDetailDto Sale() => new()
    {
        Id = Guid.NewGuid(), SaleNumber = "S1", ReturnCode = "R-A",
        SaleDate = DateTimeOffset.UtcNow, BranchOfficeName = "X", SalePersonName = "Y",
        SubTotal = 100, VatTotal = 20, GrandTotal = 120, Items = [], Payments = []
    };

    private static ReceiptTemplateDto T(string th = "[]", string a4 = "{}") => new() { ThermalJson = th, A4Json = a4 };

    [Fact]
    public async Task Render_ThermalEmpty_ReturnsWrapper()
    {
        _mgr.Setup(m => m.GetAsync()).ReturnsAsync(new SuccessDataResult<ReceiptTemplateDto>(T()));
        var html = await new ReceiptRenderer(_mgr.Object).RenderAsync(Sale(), ReceiptMode.Normal, ReceiptSize.Thermal);
        html.Should().Contain("receipt-thermal");
    }

    [Fact]
    public async Task Render_ThermalWithReturnCode_IncludesCode()
    {
        var json = """[{ "id":"x", "type":"return_code", "showInNormal":true, "showInGift":true, "settings":{"showLabel":true} }]""";
        _mgr.Setup(m => m.GetAsync()).ReturnsAsync(new SuccessDataResult<ReceiptTemplateDto>(T(th: json)));
        var html = await new ReceiptRenderer(_mgr.Object).RenderAsync(Sale(), ReceiptMode.Normal, ReceiptSize.Thermal);
        html.Should().Contain("R-A");
    }

    [Fact]
    public async Task Render_A4Empty_RendersAllFiveZones()
    {
        _mgr.Setup(m => m.GetAsync()).ReturnsAsync(new SuccessDataResult<ReceiptTemplateDto>(T()));
        var html = await new ReceiptRenderer(_mgr.Object).RenderAsync(Sale(), ReceiptMode.Normal, ReceiptSize.A4);
        html.Should().Contain("receipt-a4")
            .And.Contain("zone-hl").And.Contain("zone-hr")
            .And.Contain("zone-body")
            .And.Contain("zone-fl").And.Contain("zone-fr");
    }

    [Fact]
    public async Task Render_A4WithLogoInHl_RendersImg()
    {
        var json = """{ "hl": [{ "id":"l", "type":"logo", "showInNormal":true, "showInGift":true, "settings":{} }] }""";
        _mgr.Setup(m => m.GetAsync()).ReturnsAsync(new SuccessDataResult<ReceiptTemplateDto>(
            new ReceiptTemplateDto { ThermalJson = "[]", A4Json = json, LogoUrl = "/x.png", LogoWidthPx = 100 }));
        var html = await new ReceiptRenderer(_mgr.Object).RenderAsync(Sale(), ReceiptMode.Normal, ReceiptSize.A4);
        html.Should().Contain("/x.png");
    }

    [Fact]
    public async Task Render_TemplateMissing_ReturnsFallback()
    {
        _mgr.Setup(m => m.GetAsync()).ReturnsAsync(new ErrorDataResult<ReceiptTemplateDto>(null!, "Yok"));
        var html = await new ReceiptRenderer(_mgr.Object).RenderAsync(Sale(), ReceiptMode.Normal, ReceiptSize.Thermal);
        html.Should().Contain("Şablon yüklenemedi");
    }
}
