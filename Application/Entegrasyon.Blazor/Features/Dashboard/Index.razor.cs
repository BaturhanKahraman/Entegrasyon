using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Blazor.Features.Dashboard;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Entegrasyon.Business.Concrete;

public partial class Index : IAsyncDisposable
{
    [Inject] private IProductService? ProductManager { get; set; }
    [Inject] private ISaleManager? SaleManager { get; set; }
    [Inject] private ISnackbar? Snackbar { get; set; }

    private bool loading = true;
    private DashboardStats stats = new();
    private List<ChartSeries> salesChartData = [];
    private string[] xAxisLabels = [];
    private readonly ChartOptions chartOptions = new() { YAxisTicks = 1000, MaxNumYAxisTicks = 10 };
    private List<MarketplaceStatus> marketplaceStatuses = [];
    private List<ActivityItem> recentActivities = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadDashboardData();
    }

    private async Task LoadDashboardData()
    {
        loading = true;

        // TODO: Implement actual data loading
        // Load stats from managers
        // var productsCount = await ProductManager.GetProductsCount();
        // var todaySales = await SaleManager.GetTodaySalesAsync();

        // Mock data for now
        stats = new DashboardStats
        {
            TotalProducts = 1250,
            TodaySales = 45,
            TodayRevenue = 12450.50m,
            PendingOrders = 8,
            LowStockProducts = 12
        };

        // Sales chart data (last 7 days)
        xAxisLabels = ["Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz"];
        salesChartData =
        [
            new ChartSeries { Name = "Satışlar", Data = [15000, 18000, 22000, 19000, 25000, 28000, 24000] }
        ];

        // Marketplace statuses
        marketplaceStatuses =
        [
            new("Trendyol", Icons.Material.Filled.Store, Color.Primary, "Aktif", Color.Success),
            new("Hepsiburada", Icons.Material.Filled.ShoppingCart, Color.Secondary, "Aktif", Color.Success),
            new("N11", Icons.Material.Filled.Storefront, Color.Info, "Senkronize Ediliyor", Color.Warning),
            new("Amazon", Icons.Material.Filled.LocalMall, Color.Tertiary, "Beklemede", Color.Default)
        ];

        // Recent activities
        recentActivities =
        [
            new("Yeni ürün eklendi: Lacoste Polo T-Shirt", "2 dk önce", Color.Success),
            new("Trendyol siparişi alındı: #TR-12345", "15 dk önce", Color.Info),
            new("Stok güncellendi: 125 ürün", "1 saat önce", Color.Primary),
            new("Yeni kullanıcı kaydı: Ahmet Y.", "2 saat önce", Color.Secondary),
            new("Pazaryeri senkronizasyonu tamamlandı", "3 saat önce", Color.Success)
        ];

        await Task.Delay(500); // Simulate loading
        loading = false;
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
