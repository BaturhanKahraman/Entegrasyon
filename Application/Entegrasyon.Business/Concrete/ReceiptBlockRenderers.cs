using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using System.Text.Unicode;
using Entegrasyon.Entity.Dtos.Receipts;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Receipts;

namespace Entegrasyon.Business.Concrete;

public static class ReceiptBlockRenderers
{
    private static readonly CultureInfo TR = new("tr-TR");
    private static readonly HtmlEncoder Enc = HtmlEncoder.Create(UnicodeRanges.All);

    public static string Render(ReceiptBlockDto block, SaleDetailDto sale, ReceiptTemplateDto template, ReceiptMode mode)
    {
        var visible = mode == ReceiptMode.Gift ? block.ShowInGift : block.ShowInNormal;
        if (!visible) return "";

        return block.Type switch
        {
            ReceiptBlockTypes.Logo       => RenderLogo(template),
            ReceiptBlockTypes.StoreInfo  => RenderStoreInfo(block.Settings, template),
            ReceiptBlockTypes.Text       => RenderText(block.Settings),
            ReceiptBlockTypes.Divider    => RenderDivider(block.Settings),
            ReceiptBlockTypes.Meta       => RenderMeta(block.Settings, sale),
            ReceiptBlockTypes.Items      => RenderItems(block.Settings, sale, mode),
            ReceiptBlockTypes.Totals     => mode == ReceiptMode.Gift ? "" : RenderTotals(sale),
            ReceiptBlockTypes.Payments   => mode == ReceiptMode.Gift ? "" : RenderPayments(sale),
            ReceiptBlockTypes.VatSummary => mode == ReceiptMode.Gift ? "" : RenderVatSummary(sale),
            ReceiptBlockTypes.ReturnCode => RenderReturnCode(block.Settings, sale),
            ReceiptBlockTypes.Spacer     => RenderSpacer(block.Settings),
            _ => ""
        };
    }

    private static string RenderLogo(ReceiptTemplateDto t)
        => string.IsNullOrEmpty(t.LogoUrl)
            ? ""
            : $"<div class=\"rcpt-logo\"><img src=\"{Enc.Encode(t.LogoUrl)}\" width=\"{t.LogoWidthPx}\" alt=\"Logo\" /></div>";

    private static string RenderStoreInfo(JsonObject s, ReceiptTemplateDto t)
    {
        if (string.IsNullOrWhiteSpace(t.StoreName) && string.IsNullOrWhiteSpace(t.StoreAddress) && string.IsNullOrWhiteSpace(t.StorePhone))
            return "";
        var align = s["align"]?.GetValue<string>() ?? "center";
        var size = s["size"]?.GetValue<string>() ?? "m";
        var sb = new StringBuilder();
        sb.Append($"<div class=\"rcpt-store-info rcpt-align-{align} rcpt-size-{size}\">");
        if (!string.IsNullOrWhiteSpace(t.StoreName))    sb.Append($"<div class=\"rcpt-store-name\"><strong>{Enc.Encode(t.StoreName)}</strong></div>");
        if (!string.IsNullOrWhiteSpace(t.StoreAddress)) sb.Append($"<div>{Enc.Encode(t.StoreAddress)}</div>");
        if (!string.IsNullOrWhiteSpace(t.StorePhone))   sb.Append($"<div>{Enc.Encode(t.StorePhone)}</div>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string RenderText(JsonObject s)
    {
        var content = s["content"]?.GetValue<string>() ?? "";
        var align = s["align"]?.GetValue<string>() ?? "left";
        var bold = s["bold"]?.GetValue<bool>() ?? false;
        var size = s["size"]?.GetValue<string>() ?? "m";
        var tag = bold ? "strong" : "span";
        return $"<div class=\"rcpt-text rcpt-align-{align} rcpt-size-{size}\"><{tag}>{Enc.Encode(content)}</{tag}></div>";
    }

    private static string RenderDivider(JsonObject s)
    {
        var style = s["style"]?.GetValue<string>() ?? "dashed";
        var color = s["color"]?.GetValue<string>() ?? "black";
        return $"<div class=\"rcpt-divider rcpt-divider-{style} rcpt-divider-{color}\"></div>";
    }

    private static string RenderMeta(JsonObject s, SaleDetailDto sale)
    {
        var sn = s["showSaleNumber"]?.GetValue<bool>() ?? true;
        var sd = s["showDate"]?.GetValue<bool>() ?? true;
        var sc = s["showCashier"]?.GetValue<bool>() ?? true;
        var su = s["showCustomer"]?.GetValue<bool>() ?? true;
        var sb = new StringBuilder("<div class=\"rcpt-meta\">");
        if (sn) sb.Append($"<div class=\"rcpt-meta-row\"><span>Fiş No:</span><span>{Enc.Encode(sale.SaleNumber)}</span></div>");
        if (sd) sb.Append($"<div class=\"rcpt-meta-row\"><span>Tarih:</span><span>{sale.SaleDate.ToLocalTime().ToString("dd.MM.yyyy HH:mm", TR)}</span></div>");
        if (sc) sb.Append($"<div class=\"rcpt-meta-row\"><span>Kasiyer:</span><span>{Enc.Encode(sale.SalePersonName)}</span></div>");
        if (su && !string.IsNullOrEmpty(sale.CustomerName))
                sb.Append($"<div class=\"rcpt-meta-row\"><span>Müşteri:</span><span>{Enc.Encode(sale.CustomerName)}</span></div>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string RenderItems(JsonObject s, SaleDetailDto sale, ReceiptMode mode)
    {
        var showBarcode = s["showBarcode"]?.GetValue<bool>() ?? false;
        var showVat = s["showVatColumn"]?.GetValue<bool>() ?? false;
        var isGift = mode == ReceiptMode.Gift;
        var sb = new StringBuilder("<table class=\"rcpt-items\"><thead><tr><th>Ürün</th><th>Adet</th>");
        if (!isGift) { sb.Append("<th>Birim</th>"); if (showVat) sb.Append("<th>KDV</th>"); sb.Append("<th>Toplam</th>"); }
        sb.Append("</tr></thead><tbody>");
        foreach (var it in sale.Items)
        {
            sb.Append($"<tr><td>{Enc.Encode(it.ProductTitle)}");
            if (showBarcode && !string.IsNullOrEmpty(it.Barcode))
                sb.Append($"<div class=\"rcpt-barcode\">{Enc.Encode(it.Barcode)}</div>");
            sb.Append($"</td><td>{it.Quantity}</td>");
            if (!isGift)
            {
                sb.Append($"<td>{it.UnitPriceWithVat.ToString("C2", TR)}</td>");
                if (showVat) sb.Append($"<td>%{it.VatRate}</td>");
                sb.Append($"<td>{it.LineTotalWithVat.ToString("C2", TR)}</td>");
            }
            sb.Append("</tr>");
        }
        sb.Append("</tbody></table>");
        return sb.ToString();
    }

    private static string RenderTotals(SaleDetailDto sale)
        => "<div class=\"rcpt-totals\">" +
           $"<div class=\"rcpt-total-row\"><span>Ara Toplam:</span><span>{sale.SubTotal.ToString("C2", TR)}</span></div>" +
           $"<div class=\"rcpt-total-row\"><span>KDV:</span><span>{sale.VatTotal.ToString("C2", TR)}</span></div>" +
           $"<div class=\"rcpt-total-row rcpt-grand\"><span>TOPLAM:</span><span>{sale.GrandTotal.ToString("C2", TR)}</span></div></div>";

    private static string RenderPayments(SaleDetailDto sale)
    {
        var sb = new StringBuilder("<div class=\"rcpt-payments\"><div class=\"rcpt-payments-title\">Ödeme:</div>");
        foreach (var p in sale.Payments)
            sb.Append($"<div class=\"rcpt-payment-row\"><span>{Enc.Encode(p.PaymentMethodName)}</span><span>{p.Amount.ToString("C2", TR)}</span></div>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string RenderVatSummary(SaleDetailDto sale)
    {
        if (sale.VatSummary is null || sale.VatSummary.Count == 0) return "";
        var sb = new StringBuilder("<table class=\"rcpt-vat-summary\"><thead><tr><th>KDV</th><th>Matrah</th><th>KDV Tutarı</th></tr></thead><tbody>");
        foreach (var v in sale.VatSummary)
            sb.Append($"<tr><td>%{v.VatRate}</td><td>{v.TaxBase.ToString("C2", TR)}</td><td>{v.VatAmount.ToString("C2", TR)}</td></tr>");
        sb.Append("</tbody></table>");
        return sb.ToString();
    }

    private static string RenderReturnCode(JsonObject s, SaleDetailDto sale)
    {
        if (string.IsNullOrEmpty(sale.ReturnCode)) return "";
        var showLabel = s["showLabel"]?.GetValue<bool>() ?? true;
        var sb = new StringBuilder("<div class=\"rcpt-return-code-wrap\">");
        if (showLabel) sb.Append("<div class=\"rcpt-return-code-label\">İade Kodu</div>");
        sb.Append($"<div class=\"rcpt-return-code\">{Enc.Encode(sale.ReturnCode)}</div>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string RenderSpacer(JsonObject s)
    {
        var size = s["size"]?.GetValue<string>() ?? "m";
        return $"<div class=\"rcpt-spacer rcpt-spacer-{size}\"></div>";
    }
}
