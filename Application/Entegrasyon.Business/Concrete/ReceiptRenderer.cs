using System.Text;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Receipts;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Receipts;

namespace Entegrasyon.Business.Concrete;

public sealed class ReceiptRenderer(IReceiptTemplateManager templateManager) : IReceiptRenderer
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public async Task<string> RenderAsync(SaleDetailDto sale, ReceiptMode mode, ReceiptSize size)
    {
        var result = await templateManager.GetAsync();
        if (!result.Success || result.Data is null)
            return "<div class=\"receipt-fallback\">Şablon yüklenemedi.</div>";

        var template = result.Data;
        return size == ReceiptSize.Thermal
            ? RenderThermal(template, sale, mode)
            : RenderA4(template, sale, mode);
    }

    private static string RenderThermal(ReceiptTemplateDto t, SaleDetailDto sale, ReceiptMode mode)
    {
        var blocks = ParseThermal(t.ThermalJson);
        var sb = new StringBuilder("<div class=\"receipt-thermal\">");
        foreach (var b in blocks) sb.Append(ReceiptBlockRenderers.Render(b, sale, t, mode));
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string RenderA4(ReceiptTemplateDto t, SaleDetailDto sale, ReceiptMode mode)
    {
        var z = ParseA4(t.A4Json);
        var sb = new StringBuilder("<div class=\"receipt-a4\">");
        AppendZone(sb, "hl", z.Hl, sale, t, mode);
        AppendZone(sb, "hr", z.Hr, sale, t, mode);
        AppendZone(sb, "body", z.Body, sale, t, mode);
        AppendZone(sb, "fl", z.Fl, sale, t, mode);
        AppendZone(sb, "fr", z.Fr, sale, t, mode);
        sb.Append("</div>");
        return sb.ToString();
    }

    private static void AppendZone(StringBuilder sb, string name, List<ReceiptBlockDto> blocks,
        SaleDetailDto sale, ReceiptTemplateDto t, ReceiptMode mode)
    {
        sb.Append($"<div class=\"zone-{name}\">");
        foreach (var b in blocks) sb.Append(ReceiptBlockRenderers.Render(b, sale, t, mode));
        sb.Append("</div>");
    }

    private static List<ReceiptBlockDto> ParseThermal(string json)
    {
        try { return JsonSerializer.Deserialize<List<ReceiptBlockDto>>(json, JsonOpts) ?? []; }
        catch (JsonException) { return []; }
    }

    private static A4ReceiptBlocksDto ParseA4(string json)
    {
        try { return JsonSerializer.Deserialize<A4ReceiptBlocksDto>(json, JsonOpts) ?? new(); }
        catch (JsonException) { return new(); }
    }
}
