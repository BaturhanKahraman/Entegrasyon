using System.Text.Json.Nodes;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Dtos.Receipts;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Receipts;

namespace Entegrasyon.UnitTest.Business;

public class ReceiptBlockRenderersTests
{
    private static SaleDetailDto SampleSale() => new()
    {
        Id = Guid.NewGuid(),
        SaleNumber = "S20260419-0042",
        ReturnCode = "R-TESTCODE12345",
        SaleDate = new DateTimeOffset(2026, 4, 19, 12, 0, 0, TimeSpan.Zero),
        CustomerName = "Ahmet Yılmaz",
        SalePersonName = "Demo Kasiyer",
        BranchOfficeName = "Merkez",
        SubTotal = 250m, VatTotal = 50m, GrandTotal = 300m,
        Items =
        [
            new SaleDetailItemDto { Id = Guid.NewGuid(), ProductTitle = "Ürün A", Barcode = "123", Quantity = 1, UnitPriceWithVat = 120m, VatRate = 20m, LineTotalWithVat = 120m },
            new SaleDetailItemDto { Id = Guid.NewGuid(), ProductTitle = "Ürün B", Barcode = "456", Quantity = 2, UnitPriceWithVat = 90m, VatRate = 20m, LineTotalWithVat = 180m }
        ],
        Payments = [ new SaleDetailPaymentDto { PaymentMethodName = "Nakit", Amount = 300m, PaidAt = DateTimeOffset.UtcNow } ]
    };

    private static ReceiptTemplateDto SampleTemplate(string? logoUrl = "/logo.png") => new()
    {
        LogoUrl = logoUrl,
        LogoWidthPx = 100,
        StoreName = "Demo Mağaza",
        StoreAddress = "Cadde 1 No:5",
        StorePhone = "0555 555 55 55"
    };

    private static ReceiptBlockDto B(string type, string settingsJson = "{}") => new()
    {
        Id = Guid.NewGuid().ToString(),
        Type = type,
        ShowInNormal = true,
        ShowInGift = true,
        Settings = (JsonObject)JsonNode.Parse(settingsJson)!
    };

    [Fact]
    public void RenderLogo_WithUrl_IncludesImgTag()
    {
        var html = ReceiptBlockRenderers.Render(B("logo"), SampleSale(), SampleTemplate(), ReceiptMode.Normal);
        html.Should().Contain("<img").And.Contain("/logo.png").And.Contain("width=\"100\"");
    }

    [Fact]
    public void RenderLogo_WithoutUrl_RendersEmpty()
    {
        var html = ReceiptBlockRenderers.Render(B("logo"), SampleSale(), SampleTemplate(logoUrl: null), ReceiptMode.Normal);
        html.Should().BeEmpty();
    }

    [Fact]
    public void RenderStoreInfo_RendersAllFields()
    {
        var html = ReceiptBlockRenderers.Render(B("store_info", """{"align":"center","size":"m"}"""), SampleSale(), SampleTemplate(), ReceiptMode.Normal);
        html.Should().Contain("Demo Mağaza").And.Contain("Cadde 1 No:5").And.Contain("0555 555 55 55");
    }

    [Fact]
    public void RenderText_EncodesHtml()
    {
        var html = ReceiptBlockRenderers.Render(B("text", """{"content":"<script>alert(1)</script>","align":"left","bold":false,"size":"m"}"""), SampleSale(), SampleTemplate(), ReceiptMode.Normal);
        html.Should().NotContain("<script>alert").And.Contain("&lt;script&gt;");
    }

    [Fact]
    public void RenderDivider_DashedStyle()
        => ReceiptBlockRenderers.Render(B("divider", """{"style":"dashed","color":"black"}"""), SampleSale(), SampleTemplate(), ReceiptMode.Normal)
            .Should().Contain("dashed");

    [Fact]
    public void RenderMeta_ShowsFlaggedFieldsOnly()
    {
        var html = ReceiptBlockRenderers.Render(B("meta", """{"showSaleNumber":true,"showDate":false,"showCashier":false,"showCustomer":false}"""), SampleSale(), SampleTemplate(), ReceiptMode.Normal);
        html.Should().Contain("S20260419-0042").And.NotContain("Demo Kasiyer");
    }

    [Fact]
    public void RenderItems_ContainsAllProducts()
    {
        var html = ReceiptBlockRenderers.Render(B("items", """{"showBarcode":false,"showVatColumn":false}"""), SampleSale(), SampleTemplate(), ReceiptMode.Normal);
        html.Should().Contain("Ürün A").And.Contain("Ürün B");
    }

    [Fact]
    public void RenderTotals_InGiftMode_RendersEmpty()
        => ReceiptBlockRenderers.Render(B("totals"), SampleSale(), SampleTemplate(), ReceiptMode.Gift).Should().BeEmpty();

    [Fact]
    public void RenderTotals_InNormalMode_ContainsAmounts()
        => ReceiptBlockRenderers.Render(B("totals"), SampleSale(), SampleTemplate(), ReceiptMode.Normal).Should().Contain("TOPLAM");

    [Fact]
    public void RenderPayments_InGiftMode_RendersEmpty()
        => ReceiptBlockRenderers.Render(B("payments"), SampleSale(), SampleTemplate(), ReceiptMode.Gift).Should().BeEmpty();

    [Fact]
    public void RenderReturnCode_RendersCodeInBox()
    {
        var html = ReceiptBlockRenderers.Render(B("return_code", """{"showLabel":true}"""), SampleSale(), SampleTemplate(), ReceiptMode.Normal);
        html.Should().Contain("R-TESTCODE12345").And.Contain("İade Kodu");
    }

    [Fact]
    public void RenderReturnCode_WithoutLabel_NoLabelShown()
    {
        var html = ReceiptBlockRenderers.Render(B("return_code", """{"showLabel":false}"""), SampleSale(), SampleTemplate(), ReceiptMode.Normal);
        html.Should().Contain("R-TESTCODE12345").And.NotContain("İade Kodu");
    }

    [Fact]
    public void RenderSpacer_RendersDiv()
        => ReceiptBlockRenderers.Render(B("spacer", """{"size":"l"}"""), SampleSale(), SampleTemplate(), ReceiptMode.Normal).Should().Contain("rcpt-spacer");

    [Fact]
    public void Render_ShowInNormalFalse_NormalMode_Empty()
    {
        var block = B("text", """{"content":"hi","align":"left","bold":false,"size":"m"}""");
        block.ShowInNormal = false;
        ReceiptBlockRenderers.Render(block, SampleSale(), SampleTemplate(), ReceiptMode.Normal).Should().BeEmpty();
    }
}
