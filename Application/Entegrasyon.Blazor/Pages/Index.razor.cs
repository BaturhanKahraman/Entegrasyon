namespace Entegrasyon.Blazor.Pages;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Entegrasyon.Business.Concrete;

public partial class Index : IAsyncDisposable
{
    [Inject] private ProductManager? ProductManager { get; set; }
    [Inject] private SaleManager? SaleManager { get; set; }
    [Inject] private ISnackbar? Snackbar { get; set; }

    private bool _loading = true;
    private DashboardStats _stats = new();
    private List<ChartSeries> _salesChartData = new();
    private string[] _xAxisLabels = [];
    private ChartOptions _chartOptions = new() { YAxisTicks = 1000, MaxNumYAxisTicks = 10 };
    private List<MarketplaceStatus> _marketplaceStatuses = new();
    private List<ActivityItem> _recentActivities = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadDashboardData();
    }

    private async Task LoadDashboardData()
    {
        _loading = true;
        try
        {
            // TODO: Implement actual data loading
            // Load stats from managers
            // var productsCount = await ProductManager.GetProductsCount();
            // var todaySales = await SaleManager.GetTodaySalesAsync();

            // Mock data for now
            _stats = new DashboardStats
            {
                TotalProducts = 1250,
                TodaySales = 45,
                TodayRevenue = 12450.50m,
                PendingOrders = 8,
                LowStockProducts = 12
            };

            // Sales chart data (last 7 days)
            _xAxisLabels = ["Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz"];
            _salesChartData =
            [
                new ChartSeries { Name = "Satışlar", Data = new double[] { 15000, 18000, 22000, 19000, 25000, 28000, 24000 } }
            ];

            // Marketplace statuses
            _marketplaceStatuses =
            [
                new("Trendyol", Icons.Material.Filled.Store, Color.Primary, "Aktif", Color.Success),
                new("Hepsiburada", Icons.Material.Filled.ShoppingCart, Color.Secondary, "Aktif", Color.Success),
                new("N11", Icons.Material.Filled.Storefront, Color.Info, "Senkronize Ediliyor", Color.Warning),
                new("Amazon", Icons.Material.Filled.LocalMall, Color.Tertiary, "Beklemede", Color.Default)
            ];

            // Recent activities
            _recentActivities =
            [
                new("Yeni ürün eklendi: Lacoste Polo T-Shirt", "2 dk önce", Color.Success),
                new("Trendyol siparişi alındı: #TR-12345", "15 dk önce", Color.Info),
                new("Stok güncellendi: 125 ürün", "1 saat önce", Color.Primary),
                new("Yeni kullanıcı kaydı: Ahmet Y.", "2 saat önce", Color.Secondary),
                new("Pazaryeri senkronizasyonu tamamlandı", "3 saat önce", Color.Success)
            ];

            await Task.Delay(500); // Simulate loading
        }
        catch (Exception ex)
        {
            Snackbar?.Add($"Dashboard yüklenirken hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private void SyncMarketplaces()
    {
        Snackbar?.Add("Pazaryeri senkronizasyonu başlatıldı", Severity.Info);
        // TODO: Trigger marketplace sync
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private record DashboardStats
    {
        public int TotalProducts { get; init; }
        public int TodaySales { get; init; }
        public decimal TodayRevenue { get; init; }
        public int PendingOrders { get; init; }
        public int LowStockProducts { get; init; }
    }

    private record MarketplaceStatus(string Name, string Icon, Color Color, string Status, Color StatusColor);

    private record ActivityItem(string Message, string Time, Color Color);
}
