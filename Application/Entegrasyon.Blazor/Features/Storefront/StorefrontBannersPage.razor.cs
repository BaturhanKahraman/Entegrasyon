using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Storefront;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Storefront;

public partial class StorefrontBannersPage
{
    [Inject] private IStorefrontBannerManager BannerManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    private List<StorefrontBanner> _banners = [];
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadBannersAsync();
    }

    private async Task LoadBannersAsync()
    {
        _loading = true;
        var result = await BannerManager.GetAllBannersAsync(1);
        if (result.Success)
            _banners = result.Data;
        _loading = false;
    }

    private async Task OpenCreateDialog()
    {
        var banner = new StorefrontBanner { TenantId = 1, IsActive = true };
        var parameters = new DialogParameters
        {
            { "Banner", banner },
            { "IsEdit", false }
        };

        var dialog = await DialogService.ShowAsync<StorefrontBannerDialog>("Yeni Banner", parameters);
        var result = await dialog.Result;

        if (!result!.Canceled && result.Data is StorefrontBanner created)
        {
            var serviceResult = await BannerManager.CreateAsync(created);
            if (serviceResult.Success)
            {
                Snackbar.Add("Banner olusturuldu.", Severity.Success);
                await LoadBannersAsync();
            }
            else
            {
                Snackbar.Add(serviceResult.Message ?? "Hata olustu.", Severity.Error);
            }
        }
    }

    private async Task OpenEditDialog(StorefrontBanner banner)
    {
        var clone = new StorefrontBanner
        {
            Id = banner.Id,
            TenantId = banner.TenantId,
            Title = banner.Title,
            ImageUrl = banner.ImageUrl,
            MobileImageUrl = banner.MobileImageUrl,
            LinkUrl = banner.LinkUrl,
            Position = banner.Position,
            DisplayOrder = banner.DisplayOrder,
            StartDate = banner.StartDate,
            EndDate = banner.EndDate,
            IsActive = banner.IsActive
        };

        var parameters = new DialogParameters
        {
            { "Banner", clone },
            { "IsEdit", true }
        };

        var dialog = await DialogService.ShowAsync<StorefrontBannerDialog>("Banner Duzenle", parameters);
        var result = await dialog.Result;

        if (!result!.Canceled && result.Data is StorefrontBanner updated)
        {
            var serviceResult = await BannerManager.UpdateAsync(updated);
            if (serviceResult.Success)
            {
                Snackbar.Add("Banner guncellendi.", Severity.Success);
                await LoadBannersAsync();
            }
            else
            {
                Snackbar.Add(serviceResult.Message ?? "Hata olustu.", Severity.Error);
            }
        }
    }

    private async Task DeleteBannerAsync(StorefrontBanner banner)
    {
        var confirm = await DialogService.ShowMessageBox(
            "Banner Sil",
            $"\"{banner.Title}\" bannerini silmek istediginizden emin misiniz?",
            yesText: "Sil",
            cancelText: "Iptal");

        if (confirm == true)
        {
            var result = await BannerManager.DeleteAsync(banner.Id);
            if (result.Success)
            {
                Snackbar.Add("Banner silindi.", Severity.Success);
                await LoadBannersAsync();
            }
            else
            {
                Snackbar.Add(result.Message ?? "Hata olustu.", Severity.Error);
            }
        }
    }
}
