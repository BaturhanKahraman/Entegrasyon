using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync;

public partial class MarketplaceSync
{
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private bool _syncing;
    private readonly string _trendyolStatus = "Aktif";
    private readonly string _trendyolLastSync = "15 dk önce";
    private List<SyncHistoryItem> _syncHistory = [];

    protected override void OnInitialized()
    {
        _syncHistory =
        [
            new("Trendyol", "Ürün Senkronizasyonu", "Başarılı", 1234, "14:30", "14:45", "15 dk"),
            new("Hepsiburada", "Ürün Senkronizasyonu", "Başarılı", 856, "14:00", "14:12", "12 dk"),
            new("N11", "Sipariş Senkronizasyonu", "Başarılı", 25, "13:45", "13:48", "3 dk"),
            new("Trendyol", "Stok Güncelleme", "Başarılı", 450, "13:00", "13:05", "5 dk")
        ];
    }

    private static Color GetStatusColor(string status) => status switch
    {
        "Aktif" or "Başarılı" => Color.Success,
        "Beklemede" => Color.Warning,
        "Hatalı" => Color.Error,
        _ => Color.Info
    };

    private async Task SyncMarketplace(string marketplace)
    {
        _syncing = true;
        Snackbar.Add($"{marketplace} senkronizasyonu başlatıldı", Severity.Info);

        try
        {
            // TODO: Trigger actual sync via background service
            await Task.Delay(2000);
            Snackbar.Add($"{marketplace} senkronizasyonu tamamlandı", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Senkronizasyon hatası: {ex.Message}", Severity.Error);
        }
        finally
        {
            _syncing = false;
        }
    }

    private void ConfigureMarketplace(string marketplace)
    {
        Snackbar.Add($"{marketplace} ayarları yakında gelecek", Severity.Info);
    }

    private record SyncHistoryItem(
        string Marketplace,
        string Type,
        string Status,
        int ItemCount,
        string StartTime,
        string EndTime,
        string Duration
    );
}
