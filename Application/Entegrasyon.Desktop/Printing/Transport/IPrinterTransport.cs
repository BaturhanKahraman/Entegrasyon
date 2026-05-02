using Entegrasyon.PrintAgent.Contracts;

namespace Entegrasyon.Desktop.Printing.Transport;

public interface IPrinterTransport
{
    Task<PrintResult> SendRawAsync(string printerIdentifier, byte[] data, CancellationToken ct);
    Task<PrinterStatus> GetStatusAsync(string printerIdentifier, CancellationToken ct);
    Task<IReadOnlyList<DiscoveredPrinter>> DiscoverAsync(CancellationToken ct);
}
