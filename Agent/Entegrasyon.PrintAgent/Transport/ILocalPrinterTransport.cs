using Entegrasyon.PrintAgent.Contracts;

namespace Entegrasyon.PrintAgent.Transport;

public interface ILocalPrinterTransport
{
    Task<PrintResult> SendRawAsync(string printerName, byte[] data, CancellationToken ct);
    Task<PrinterStatus> GetStatusAsync(string printerName, CancellationToken ct);
    Task<IReadOnlyList<DiscoveredPrinter>> DiscoverAsync(CancellationToken ct);
}
