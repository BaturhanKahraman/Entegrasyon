namespace Entegrasyon.Desktop.Printing.Configuration;

public class PrinterConfiguration
{
    public List<PrinterEntry> Printers { get; set; } = [];
}

public class PrinterEntry
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Network"; // Network | USB
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; } = 9100;
    public bool IsDefault { get; set; }
}
