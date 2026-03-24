namespace Entegrasyon.Entity.Dtos.Settings;

public record BarcodeScannerConfig
{
    public bool Enabled { get; init; } = true;
    public int Timeout { get; init; } = 100;
    public int MinLength { get; init; } = 6;
    public string DefaultAction { get; init; } = "SalesAdd";
}
