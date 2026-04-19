namespace Entegrasyon.Entity.Dtos.Receipts;

public sealed class ReceiptTemplateDto
{
    public string ThermalJson { get; set; } = "[]";
    public string A4Json { get; set; } = "{}";
    public string? LogoUrl { get; set; }
    public int LogoWidthPx { get; set; } = 120;
    public string StoreName { get; set; } = "";
    public string StoreAddress { get; set; } = "";
    public string StorePhone { get; set; } = "";
}
