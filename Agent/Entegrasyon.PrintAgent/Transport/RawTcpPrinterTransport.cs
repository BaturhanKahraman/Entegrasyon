using System.Net.Sockets;
using Entegrasyon.PrintAgent.Configuration;
using Entegrasyon.PrintAgent.Contracts;
using Microsoft.Extensions.Options;

namespace Entegrasyon.PrintAgent.Transport;

public class RawTcpPrinterTransport(
    IOptions<PrinterConfiguration> config,
    ILogger<RawTcpPrinterTransport> logger) : IPrinterTransport
{
    public async Task<PrintResult> SendRawAsync(string printerIdentifier, byte[] data, CancellationToken ct)
    {
        var printer = config.Value.Printers.FirstOrDefault(p =>
            p.Name.Equals(printerIdentifier, StringComparison.OrdinalIgnoreCase)
            && p.Type.Equals("Network", StringComparison.OrdinalIgnoreCase));

        if (printer is null)
            return new PrintResult(false, $"Ağ yazıcısı bulunamadı: {printerIdentifier}");

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(printer.Address, printer.Port, ct);
            await client.GetStream().WriteAsync(data, ct);
            await client.GetStream().FlushAsync(ct);

            logger.LogInformation("Veri gönderildi: {PrinterName} ({Address}:{Port}), {ByteCount} byte",
                printer.Name, printer.Address, printer.Port, data.Length);

            return new PrintResult(true, "Yazdırma komutu gönderildi");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Yazıcıya bağlanılamadı: {PrinterName} ({Address}:{Port})",
                printer.Name, printer.Address, printer.Port);
            return new PrintResult(false, $"Yazıcıya bağlanılamadı: {ex.Message}");
        }
    }

    public async Task<PrinterStatus> GetStatusAsync(string printerIdentifier, CancellationToken ct)
    {
        var printer = config.Value.Printers.FirstOrDefault(p =>
            p.Name.Equals(printerIdentifier, StringComparison.OrdinalIgnoreCase)
            && p.Type.Equals("Network", StringComparison.OrdinalIgnoreCase));

        if (printer is null)
            return new PrinterStatus(printerIdentifier, false, "Yazıcı tanımlı değil");

        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            await client.ConnectAsync(printer.Address, printer.Port, cts.Token);
            return new PrinterStatus(printer.Name, true, "Çevrimiçi");
        }
        catch
        {
            return new PrinterStatus(printer.Name, false, "Çevrimdışı");
        }
    }

    public Task<IReadOnlyList<DiscoveredPrinter>> DiscoverAsync(CancellationToken ct)
    {
        var printers = config.Value.Printers
            .Where(p => p.Type.Equals("Network", StringComparison.OrdinalIgnoreCase))
            .Select(p => new DiscoveredPrinter(p.Name, "Network", p.Address, p.IsDefault))
            .ToList();

        return Task.FromResult<IReadOnlyList<DiscoveredPrinter>>(printers);
    }
}
