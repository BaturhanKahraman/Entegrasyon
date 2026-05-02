namespace Entegrasyon.Entity.Printing;

public enum PrintBatchItemStatus
{
    Pending = 0,
    Printed = 1,
    Failed = 2
}

public sealed class PrintBatchItem : BaseEntity
{
    public int Id { get; set; }
    public Guid PrintBatchId { get; set; }
    public PrintBatch PrintBatch { get; set; } = null!;

    public int Order { get; set; }
    public string PrinterLanguage { get; set; } = "ZPL";
    public string ZplContent { get; set; } = string.Empty;
    public byte[]? RawBytes { get; set; }
    public string Description { get; set; } = string.Empty;

    public PrintBatchItemStatus Status { get; set; }
    public DateTimeOffset? PrintedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
