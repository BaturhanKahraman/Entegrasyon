using WireMock.Server;
using WireMock.Settings;

namespace Entegrasyon.IntegrationTest.Fixtures;

/// <summary>
/// xUnit shared fixture — in-process WireMockServer.
/// Integration test collection'inda PostgreSqlFixture ile birlikte paylasilir.
/// Her integration test class'inin OnInitializeAsync metodunda ResetAll() cagrilip
/// marketplace stub'lari kurulmali, ardindan MarketPlace seed'inde BaseUrl bu fixture'in
/// BaseUrl'ine atanmali.
/// </summary>
public sealed class WireMockFixture : IAsyncLifetime
{
    public WireMockServer Server { get; private set; } = null!;

    /// <summary>
    /// Mock server'in taban URL'i. MarketPlace.BaseUrl'e yazilarak runtime'da
    /// gercek marketplace HTTP client'lari bu sunucuya yonlendirilir.
    /// </summary>
    public string BaseUrl => Server.Url!.TrimEnd('/');

    public Task InitializeAsync()
    {
        Server = WireMockServer.Start(new WireMockServerSettings
        {
            Port = null,
            StartAdminInterface = true,
            ReadStaticMappings = false,
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
    /// Her integration test class'inin setup'inda cagrilmali.
    /// </summary>
    public void ResetAll() => Server.Reset();
}
