using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Settings;

public partial class IntegrationSettings : ComponentBase
{
    [Inject] private IMarketPlaceManager MarketPlaceManager { get; set; } = null!;
    [Inject] private IBranchOfficeManager BranchOfficeManager { get; set; } = null!;
    [Inject] private IConfiguration Configuration { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private const int TrendyolMarketPlaceId = 1;

    private bool _loading = true;
    private bool _saving;
    private bool _testing;
    private bool _savingWarehouses;
    private bool _showSecret;
    private bool? _connectionTested;
    private bool _isMockMode;

    // Trendyol credentials
    private string _sellerId = string.Empty;
    private string _apiKey = string.Empty;
    private string _apiSecret = string.Empty;
    private string _baseUrl = "https://apigw.trendyol.com";

    // Depo seçimi
    private List<BranchOffice> _branches = [];
    private HashSet<int> _selectedBranchIds = [];

    protected override async Task OnInitializedAsync()
    {
        _isMockMode = Configuration.GetValue<bool>("Trendyol:UseMock", true);
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _loading = true;

        // Marketplace bilgilerini yükle
        var mpResult = await MarketPlaceManager.GetByIdAsync(TrendyolMarketPlaceId);
        if (mpResult.Success && mpResult.Data is not null)
        {
            var mp = mpResult.Data;
            _sellerId = mp.SellerId ?? string.Empty;
            _apiKey = mp.ApiKey ?? string.Empty;
            _apiSecret = mp.ApiSecret ?? string.Empty;
            _baseUrl = mp.BaseUrl ?? "https://apigw.trendyol.com";
        }

        // Depoları yükle
        var branchResult = await BranchOfficeManager.GetBranchList();
        if (branchResult.Success)
            _branches = branchResult.Data;

        // Seçili depoları yükle
        var whResult = await MarketPlaceManager.GetWarehousesAsync(TrendyolMarketPlaceId);
        if (whResult.Success)
            _selectedBranchIds = whResult.Data.Select(w => w.BranchOfficeId).ToHashSet();

        _loading = false;
    }

    private async Task SaveCredentialsAsync()
    {
        if (string.IsNullOrWhiteSpace(_sellerId))
        {
            Snackbar.Add("Satıcı ID boş olamaz.", Severity.Warning);
            return;
        }

        _saving = true;
        var result = await MarketPlaceManager.UpdateCredentialsAsync(
            TrendyolMarketPlaceId, _apiKey, _apiSecret, _sellerId, _baseUrl);

        Snackbar.Add(result.Message, result.Success ? Severity.Success : Severity.Error);
        _connectionTested = null; // Reset test status
        _saving = false;
    }

    private async Task TestConnectionAsync()
    {
        _testing = true;
        _connectionTested = null;

        var result = await MarketPlaceManager.TestConnectionAsync(TrendyolMarketPlaceId);
        _connectionTested = result.Data;

        Snackbar.Add(result.Message, result.Success ? Severity.Success : Severity.Error);
        _testing = false;
    }

    private async Task SaveWarehousesAsync()
    {
        _savingWarehouses = true;
        var result = await MarketPlaceManager.SetWarehousesAsync(
            TrendyolMarketPlaceId, _selectedBranchIds.ToList());

        Snackbar.Add(result.Message, result.Success ? Severity.Success : Severity.Error);
        _savingWarehouses = false;
    }

    private void ToggleBranch(int branchId, bool selected)
    {
        if (selected)
            _selectedBranchIds.Add(branchId);
        else
            _selectedBranchIds.Remove(branchId);
    }
}
