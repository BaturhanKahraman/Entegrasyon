using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Receipts;

public sealed class ReceiptTemplate : BaseEntity
{
    public int Id { get; set; }
    public string ThermalJson { get; set; } = "[]";
    public string A4Json { get; set; } = "{}";

    [StringLength(500)]
    public string? LogoUrl { get; set; }

    public int LogoWidthPx { get; set; } = 120;

    [StringLength(100)]
    public string StoreName { get; set; } = "";

    [StringLength(200)]
    public string StoreAddress { get; set; } = "";

    [StringLength(20)]
    public string StorePhone { get; set; } = "";
}
