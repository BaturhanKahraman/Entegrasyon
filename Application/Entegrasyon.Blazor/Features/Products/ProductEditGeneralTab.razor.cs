using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Products;

public partial class ProductEditGeneralTab
{
    [Parameter] public List<BrandListDetailDto> Brands { get; set; } = [];
    [Parameter] public List<CategorySelectDto> Categories { get; set; } = [];

    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> TitleChanged { get; set; }

    [Parameter] public string Description { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> DescriptionChanged { get; set; }

    [Parameter] public string StockCode { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> StockCodeChanged { get; set; }

    [Parameter] public string Season { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> SeasonChanged { get; set; }

    [Parameter] public string Year { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> YearChanged { get; set; }

    [Parameter] public int BrandId { get; set; }
    [Parameter] public EventCallback<int> BrandIdChanged { get; set; }

    [Parameter] public int CategoryId { get; set; }
    [Parameter] public EventCallback<int> CategoryIdChanged { get; set; }

    [Inject] private IDialogService DialogService { get; set; } = null!;

    private async Task OnCategoryChangedInternal(int newId)
    {
        if (newId == CategoryId) return;

        var confirm = await DialogService.ShowMessageBox(
            "Kategori Değişikliği",
            "Kategori değiştirilirse mevcut özellikler temizlenecektir. Devam etmek istiyor musunuz?",
            yesText: "Evet, Değiştir",
            cancelText: "İptal");

        if (confirm == true)
            await CategoryIdChanged.InvokeAsync(newId);
        else
            StateHasChanged();
    }
}
