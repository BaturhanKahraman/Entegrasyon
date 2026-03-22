using System.Text;
using Entegrasyon.PrintAgent.Configuration;
using Entegrasyon.PrintAgent.Contracts;
using Entegrasyon.PrintAgent.Transport;
using Microsoft.Extensions.Options;

namespace Entegrasyon.PrintAgent;

public class PrintService(
    IPrinterTransport networkTransport,
    ILocalPrinterTransport localTransport,
    IOptions<PrinterConfiguration> config,
    ILogger<PrintService> logger)
{
    public async Task<PrintJobResponse> ProcessJobAsync(PrintJobRequest request, CancellationToken ct)
    {
        var jobId = Guid.NewGuid().ToString("N")[..8];
        logger.LogInformation("Yazdırma işi başlatıldı: {JobId}, Yazıcı: {Printer}, Kopya: {Copies}",
            jobId, request.TargetPrinter, request.Copies);

        byte[] data = request.Language == PrinterLanguage.ESCPOS && request.RawBytes is { Length: > 0 }
            ? request.RawBytes
            : Encoding.UTF8.GetBytes(request.ZplContent ?? string.Empty);

        // Önce config'den yazıcıyı ara (gerçek mod)
        var configPrinter = config.Value.Printers.FirstOrDefault(p =>
            p.Name.Equals(request.TargetPrinter, StringComparison.OrdinalIgnoreCase));

        for (int i = 0; i < request.Copies; i++)
        {
            PrintResult result;

            if (configPrinter is not null)
            {
                // Config'de tanımlı — tipine göre transport seç
                result = configPrinter.Type.Equals("Network", StringComparison.OrdinalIgnoreCase)
                    ? await networkTransport.SendRawAsync(request.TargetPrinter, data, ct)
                    : await localTransport.SendRawAsync(request.TargetPrinter, data, ct);
            }
            else
            {
                // Config'de yok — keşfedilmiş yazıcılarda ara (mock mod dahil)
                var allPrinters = await GetAllPrintersAsync(ct);
                var discovered = allPrinters.FirstOrDefault(p =>
                    p.Name.Equals(request.TargetPrinter, StringComparison.OrdinalIgnoreCase));

                if (discovered is null)
                    return new PrintJobResponse(jobId, $"error: Yazıcı bulunamadı: {request.TargetPrinter}");

                result = discovered.ConnectionType == "Network"
                    ? await networkTransport.SendRawAsync(request.TargetPrinter, data, ct)
                    : await localTransport.SendRawAsync(request.TargetPrinter, data, ct);
            }

            if (!result.Success)
                return new PrintJobResponse(jobId, $"error: {result.Message}");
        }

        return new PrintJobResponse(jobId, "completed");
    }

    public async Task<IReadOnlyList<DiscoveredPrinter>> GetAllPrintersAsync(CancellationToken ct)
    {
        var networkPrinters = await networkTransport.DiscoverAsync(ct);
        var localPrinters = await localTransport.DiscoverAsync(ct);

        // Aynı isimde yazıcıları tekilleştir (mock modda her iki transport da aynı instance)
        return networkPrinters
            .Concat(localPrinters)
            .DistinctBy(p => p.Name)
            .ToList();
    }

    public async Task<PrinterStatus> GetPrinterStatusAsync(string printerName, CancellationToken ct)
    {
        var configPrinter = config.Value.Printers.FirstOrDefault(p =>
            p.Name.Equals(printerName, StringComparison.OrdinalIgnoreCase));

        if (configPrinter is not null)
        {
            return configPrinter.Type.Equals("Network", StringComparison.OrdinalIgnoreCase)
                ? await networkTransport.GetStatusAsync(printerName, ct)
                : await localTransport.GetStatusAsync(printerName, ct);
        }

        // Config'de yoksa mock/discovered olabilir — network transport'tan dene
        return await networkTransport.GetStatusAsync(printerName, ct);
    }
}
