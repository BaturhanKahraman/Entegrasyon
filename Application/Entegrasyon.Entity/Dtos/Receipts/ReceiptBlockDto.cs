using System.Text.Json.Nodes;

namespace Entegrasyon.Entity.Dtos.Receipts;

public sealed class ReceiptBlockDto
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public bool ShowInNormal { get; set; } = true;
    public bool ShowInGift { get; set; } = true;
    public JsonObject Settings { get; set; } = new();
}

public sealed class A4ReceiptBlocksDto
{
    public List<ReceiptBlockDto> Hl { get; set; } = [];
    public List<ReceiptBlockDto> Hr { get; set; } = [];
    public List<ReceiptBlockDto> Body { get; set; } = [];
    public List<ReceiptBlockDto> Fl { get; set; } = [];
    public List<ReceiptBlockDto> Fr { get; set; } = [];
}

public static class ReceiptBlockTypes
{
    public const string Logo = "logo";
    public const string StoreInfo = "store_info";
    public const string Text = "text";
    public const string Divider = "divider";
    public const string Meta = "meta";
    public const string Items = "items";
    public const string Totals = "totals";
    public const string Payments = "payments";
    public const string VatSummary = "vat_summary";
    public const string ReturnCode = "return_code";
    public const string Spacer = "spacer";

    public static readonly HashSet<string> All =
    [
        Logo, StoreInfo, Text, Divider, Meta, Items, Totals, Payments, VatSummary, ReturnCode, Spacer
    ];
}
