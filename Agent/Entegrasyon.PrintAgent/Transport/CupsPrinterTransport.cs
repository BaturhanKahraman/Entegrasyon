using System.Diagnostics;
using System.Runtime.Versioning;
using Entegrasyon.PrintAgent.Contracts;

namespace Entegrasyon.PrintAgent.Transport;

[UnsupportedOSPlatform("windows")]
public class CupsPrinterTransport(ILogger<CupsPrinterTransport> logger) : ILocalPrinterTransport
{
    public async Task<PrintResult> SendRawAsync(string printerName, byte[] data, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo("lp", $"-d \"{printerName}\" -o raw")
            {
                RedirectStandardInput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process is null)
                return new PrintResult(false, "lp komutu başlatılamadı");

            await process.StandardInput.BaseStream.WriteAsync(data, ct);
            process.StandardInput.Close();

            await process.WaitForExitAsync(ct);
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(ct);
                logger.LogError("lp hatası (exit {ExitCode}): {Error}", process.ExitCode, error);
                return new PrintResult(false, $"lp hatası: {error}");
            }

            logger.LogInformation("CUPS yazıcıya gönderildi: {Printer}, {ByteCount} byte", printerName, data.Length);
            return new PrintResult(true, "Yazdırma komutu gönderildi");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CUPS yazdırma hatası: {Printer}", printerName);
            return new PrintResult(false, $"Yazdırma hatası: {ex.Message}");
        }
    }

    public async Task<PrinterStatus> GetStatusAsync(string printerName, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo("lpstat", $"-p \"{printerName}\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process is null)
                return new PrinterStatus(printerName, false, "lpstat çalıştırılamadı");

            var output = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            var isOnline = output.Contains("idle", StringComparison.OrdinalIgnoreCase)
                        || output.Contains("enabled", StringComparison.OrdinalIgnoreCase);

            return new PrinterStatus(printerName, isOnline, isOnline ? "Çevrimiçi" : "Çevrimdışı");
        }
        catch
        {
            return new PrinterStatus(printerName, false, "Durum alınamadı");
        }
    }

    public async Task<IReadOnlyList<DiscoveredPrinter>> DiscoverAsync(CancellationToken ct)
    {
        var printers = new List<DiscoveredPrinter>();

        try
        {
            var psi = new ProcessStartInfo("lpstat", "-p")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process is null) return printers;

            var output = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!line.StartsWith("printer ", StringComparison.OrdinalIgnoreCase)) continue;
                var parts = line.Split(' ');
                if (parts.Length >= 2)
                    printers.Add(new DiscoveredPrinter(parts[1], "USB/Local", null, false));
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "CUPS yazıcı keşfi başarısız");
        }

        return printers;
    }
}
