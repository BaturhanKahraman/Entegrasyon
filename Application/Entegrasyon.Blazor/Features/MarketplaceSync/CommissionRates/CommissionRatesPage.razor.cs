using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Marketplace;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.CommissionRates;

public partial class CommissionRatesPage
{
    [Inject] private ICommissionCalculator CommissionCalculator { get; set; } = null!;
    [Inject] private IMarketPlaceManager MarketPlaceManager { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private List<MarketPlace> _marketplaces = [];
    private List<CommissionRateRow> _rates = [];
    private int _selectedMarketPlaceId;
    private bool _loading;

    protected override async Task OnInitializedAsync()
    {
        var result = await MarketPlaceManager.GetAllAsync();
        if (result.Success)
            _marketplaces = result.Data;
    }

    private async Task OnMarketplaceChanged(int marketPlaceId)
    {
        _selectedMarketPlaceId = marketPlaceId;
        await LoadRatesAsync();
    }

    private async Task LoadRatesAsync()
    {
        if (_selectedMarketPlaceId == 0) return;

        _loading = true;
        try
        {
            var result = await CommissionCalculator.GetCommissionRatesAsync(_selectedMarketPlaceId);
            if (result.Success)
            {
                _rates = result.Data.Select(r => new CommissionRateRow
                {
                    Id = r.Id,
                    CategoryName = r.IsDefault ? "(Varsayılan)" : (r.CategoryName ?? "-"),
                    CommissionPercent = r.CommissionPercent,
                    ServiceFeePercent = r.ServiceFeePercent ?? 0,
                    TransactionFeeFixed = r.TransactionFeeFixed ?? 0,
                    Description = r.Description ?? "",
                    IsDefault = r.IsDefault,
                    CategoryId = r.CategoryId,
                    MarketPlaceId = r.MarketPlaceId
                }).ToList();
            }
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task OpenAddDialog()
    {
        var parameters = new DialogParameters<CommissionRateDialog>
        {
            { x => x.MarketPlaceId, _selectedMarketPlaceId },
            { x => x.Dto, new SaveCommissionRateDto { MarketPlaceId = _selectedMarketPlaceId } }
        };

        var dialog = await DialogService.ShowAsync<CommissionRateDialog>("Komisyon Oranı Ekle", parameters,
            new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true });
        var dialogResult = await dialog.Result;

        if (dialogResult is { Canceled: false })
            await LoadRatesAsync();
    }

    private async Task OpenEditDialog(CommissionRateRow row)
    {
        var dto = new SaveCommissionRateDto
        {
            Id = row.Id,
            MarketPlaceId = row.MarketPlaceId,
            CategoryId = row.CategoryId,
            CommissionPercent = row.CommissionPercent,
            ServiceFeePercent = row.ServiceFeePercent > 0 ? row.ServiceFeePercent : null,
            TransactionFeeFixed = row.TransactionFeeFixed > 0 ? row.TransactionFeeFixed : null,
            Description = row.Description,
            IsDefault = row.IsDefault
        };

        var parameters = new DialogParameters<CommissionRateDialog>
        {
            { x => x.MarketPlaceId, _selectedMarketPlaceId },
            { x => x.Dto, dto }
        };

        var dialog = await DialogService.ShowAsync<CommissionRateDialog>("Komisyon Oranı Düzenle", parameters,
            new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true });
        var dialogResult = await dialog.Result;

        if (dialogResult is { Canceled: false })
            await LoadRatesAsync();
    }

    private async Task DeleteRate(CommissionRateRow row)
    {
        var confirmed = await DialogService.ShowMessageBox(
            "Komisyon Oranı Sil",
            $"Bu komisyon oranını silmek istediğinize emin misiniz?",
            yesText: "Sil", cancelText: "İptal");

        if (confirmed == true)
        {
            var result = await CommissionCalculator.DeleteCommissionRateAsync(row.Id);
            if (result.Success)
            {
                Snackbar.Add("Komisyon oranı silindi.", Severity.Success);
                await LoadRatesAsync();
            }
            else
            {
                Snackbar.Add(result.Message ?? "Hata oluştu.", Severity.Error);
            }
        }
    }

    internal sealed class CommissionRateRow
    {
        public int Id { get; set; }
        public int MarketPlaceId { get; set; }
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public decimal CommissionPercent { get; set; }
        public decimal ServiceFeePercent { get; set; }
        public decimal TransactionFeeFixed { get; set; }
        public string Description { get; set; } = "";
        public bool IsDefault { get; set; }
    }
}
