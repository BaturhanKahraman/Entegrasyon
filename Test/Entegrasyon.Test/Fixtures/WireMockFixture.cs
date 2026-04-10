using WireMock.Server;
using WireMock.Settings;

namespace Entegrasyon.Test.Fixtures;

/// <summary>
/// xUnit shared fixture — in-process WireMockServer.
/// Collection fixture pattern ile test class'lari arasinda tek instance paylasilir.
/// Her test class'i baslangicinda ResetAll() cagirip kendi stub'larini kurmali.
/// Port 0 → OS dinamik secer (CI paralel runner'larinda cakisma yok).
/// </summary>
public sealed class WireMockFixture : IAsyncLifetime
{
    public WireMockServer Server { get; private set; } = null!;

    /// <summary>
    /// Mock server'in taban URL'i (orn. "http://localhost:52341").
    /// Test setup'inda MarketPlace.BaseUrl veya HttpClient.BaseAddress'e verilir.
    /// </summary>
    public string BaseUrl => Server.Url!.TrimEnd('/');

    public Task InitializeAsync()
    {
        Server = WireMockServer.Start(new WireMockServerSettings
        {
            // Port=null → OS dinamik port atar. Paralel test runner'larinda cakisma olmaz.
            Port = null,
            StartAdminInterface = true,
            // Static mapping dosyasi yok — stub'lari test kodunda kuracagiz
            ReadStaticMappings = false,
            // 127.0.0.1'e bind — bazi CI runner'larinda firewall IPv6'yi engelliyor
            UseSSL = false
        });
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Server?.Stop();
        Server?.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Tum mapping'leri, request log'larini ve scenario state'lerini temizler.
    /// Her test class'inin setup'inda cagrilmali — class'lar arasi cross-contamination'i onler.
    /// </summary>
    public void ResetAll() => Server.Reset();
}
