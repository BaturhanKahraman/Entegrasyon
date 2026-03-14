using Entegrasyon.PrintAgent.Contracts;

namespace Entegrasyon.PrintAgent.Transport;

public class MockPrinterTransport(ILogger<MockPrinterTransport> logger) : IPrinterTransport, ILocalPrinterTransport
{
    private static readonly List<DiscoveredPrinter> MockPrinters =
    [
        new("Mock-Zebra-ZD420", "Network", "192.168.1.50", true),
        new("Mock-USB-Printer", "USB/Local", null, false),
        new("Mock-Receipt-Printer", "USB/Local", null, false)
    ];

    public Task<PrintResult> SendRawAsync(string printerIdentifier, byte[] data, CancellationToken ct)
    {
        logger.LogInformation(
            "[MOCK] Yazdırma simülasyonu: Yazıcı={Printer}, Boyut={Size} byte",
            printerIdentifier, data.Length);

        // Mock modda veriyi log'a yaz — gerçek yazıcıya göndermez
        if (data.Length < 2000)
        {
            var preview = System.Text.Encoding.UTF8.GetString(data);
            logger.LogDebug("[MOCK] İçerik önizleme:\n{Content}", preview);
        }

        return Task.FromResult(new PrintResult(true, $"[MOCK] Yazdırma simüle edildi: {printerIdentifier}"));
    }

    public Task<PrinterStatus> GetStatusAsync(string printerIdentifier, CancellationToken ct)
    {
        logger.LogInformation("[MOCK] Durum sorgusu: {Printer}", printerIdentifier);
        return Task.FromResult(new PrinterStatus(printerIdentifier, true, "Çevrimiçi (Simülasyon)"));
    }

    public Task<IReadOnlyList<DiscoveredPrinter>> DiscoverAsync(CancellationToken ct)
    {
        logger.LogInformation("[MOCK] Yazıcı keşfi: {Count} sanal yazıcı", MockPrinters.Count);
        return Task.FromResult<IReadOnlyList<DiscoveredPrinter>>(MockPrinters);
    }
}
