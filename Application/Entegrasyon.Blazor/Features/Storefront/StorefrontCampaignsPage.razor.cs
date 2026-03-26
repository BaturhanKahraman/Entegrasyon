using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Storefront;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Storefront;

public partial class StorefrontCampaignsPage
{
    [Inject] private IStorefrontSettingsManager SettingsManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private StorefrontSettings _settings = new();
    private bool _loading = true;
    private bool _saving;

    protected override async Task OnInitializedAsync()
    {
        var result = await SettingsManager.GetByTenantIdAsync(1);
        if (result.Success)
            _settings = result.Data;
        else
            _settings = new StorefrontSettings { TenantId = 1 };

        _loading = false;
    }

    private async Task SaveAsync()
    {
        _saving = true;

        var result = await SettingsManager.CreateOrUpdateAsync(_settings);
        if (result.Success)
            Snackbar.Add("Kampanya ayarlari kaydedildi.", Severity.Success);
        else
            Snackbar.Add(result.Message ?? "Bir hata olustu.", Severity.Error);

        _saving = false;
    }
}
