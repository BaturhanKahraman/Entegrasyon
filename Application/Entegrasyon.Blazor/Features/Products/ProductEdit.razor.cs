using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Attributes;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Products;

public partial class ProductEdit
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IProductService ProductManager { get; set; } = null!;
    [Inject] private IImageManager ImageManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    private ProductEditPageDto? _pageData;
    private bool _loading = true;
    private bool _saving;

    private string _title = string.Empty;
    private string _description = string.Empty;
    private string _stockCode = string.Empty;
    private string _season = string.Empty;
    private string _year = string.Empty;
    private int _brandId;
    private int _categoryId;
    private List<VariantPriceModel> _variants = [];
    private List<ImageEditState> _images = [];
    private List<IBrowserFile> _newImages = [];
    private List<AttributeKeyValueDto> _attributeKeyValues = [];

    private Color _syncColor => _pageData?.SyncStatus.State switch
    {
        MarketplaceSyncState.Synced => Color.Success,
        MarketplaceSyncState.OutOfSync => Color.Warning,
        MarketplaceSyncState.Processing => Color.Info,
        MarketplaceSyncState.Waiting => Color.Warning,
        MarketplaceSyncState.Failed or MarketplaceSyncState.Rejected => Color.Error,
        _ => Color.Default
    };

    private string _syncLabel => _pageData?.SyncStatus.State switch
    {
        MarketplaceSyncState.NeverSynced => "Senkronize Edilmedi",
        MarketplaceSyncState.Waiting => "Bekliyor",
        MarketplaceSyncState.Processing => "Gönderildi — İşlemde",
        MarketplaceSyncState.OutOfSync => "Güncelleme Gerekiyor",
        MarketplaceSyncState.Synced => "Yayında",
        MarketplaceSyncState.Failed => "Başarısız",
        MarketplaceSyncState.Rejected => "Reddedildi",
        _ => string.Empty
    };

    protected override async Task OnInitializedAsync()
    {
        _loading = true;
        var result = await ProductManager.GetProductEditPageData(Id);
        if (!result.Success || result.Data is null)
        {
            Snackbar.Add(result.Message ?? "Ürün bulunamadı.", Severity.Error);
            _loading = false;
            NavigationManager.NavigateTo("/products");
            return;
        }

        _pageData = result.Data;
        var p = _pageData.Product;

        _title = p.Title;
        _description = p.Description;
        _stockCode = p.StockCode;
        _season = p.Season;
        _year = p.Year;
        _brandId = p.BrandId;
        _categoryId = p.CategoryId;

        _variants = p.ProductVariants.Select(pv => new VariantPriceModel
        {
            Id = pv.Id,
            Barcode = pv.Barcode,
            Label = BuildVariantLabel(pv),
            ListPrice = pv.ListPrice,
            SalePrice = pv.SalePrice,
            CostPrice = pv.CostPrice,
            ECommercePrice = pv.ECommercePrice,
            DimensionalWeight = pv.DimensionalWeight,
            VatRate = pv.VatRate,
            CurrencyType = pv.CurrencyType,
            CurrentStocks = pv.BranchOfficeStocks
        }).ToList();

        _images = p.ProductVariants
            .SelectMany(pv => pv.UploadedImages.Select(img => new ImageEditState
            {
                Id = img.Id,
                Src = img.Src,
                IsMain = img.IsMain,
                IsDeleted = img.IsDeleted,
                VariantId = pv.Id
            }))
            .ToList();

        _attributeKeyValues = p.AttributeKeyValues.ToList();
        _loading = false;
    }

    private void OnCategoryChanged(int newCategoryId)
    {
        if (newCategoryId == _categoryId) return;
        _categoryId = newCategoryId;
        _attributeKeyValues = [];
    }

    private async Task Save()
    {
        _saving = true;
        try
        {
            var editDto = new EditProductDto(
                Id: Id,
                Title: _title,
                Description: _description,
                StockCode: _stockCode,
                Season: _season,
                Year: _year,
                BrandId: _brandId,
                CategoryId: _categoryId,
                Variants: _variants.Select(v => new EditProductVariantDto(
                    v.Id, v.ListPrice, v.SalePrice, v.CostPrice, v.ECommercePrice,
                    v.DimensionalWeight, v.VatRate, v.CurrencyType)).ToList(),
                AttributeKeyValues: _attributeKeyValues,
                DeletedImageIds: _images.Where(i => i.IsDeleted).Select(i => i.Id).ToList()
            );

            var result = await ProductManager.UpdateProduct(editDto);
            if (!result.Success)
            {
                Snackbar.Add(result.Message ?? "Güncelleme başarısız.", Severity.Error);
                return;
            }

            if (_newImages.Count > 0 && _variants.Count > 0)
            {
                var firstVariantId = _variants[0].Id;
                var streams = _newImages.Select(f => new VariantImageStream(
                    firstVariantId,
                    f.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024),
                    f.Name,
                    IsMain: false));
                var imgResult = await ImageManager.AddProductImages(Id, streams);
                if (!imgResult.Success)
                    Snackbar.Add($"Görseller yüklenemedi: {imgResult.Message}", Severity.Warning);
            }

            Snackbar.Add("Ürün güncellendi.", Severity.Success);
            NavigationManager.NavigateTo("/products");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            _saving = false;
        }
    }

    private void Cancel() => NavigationManager.NavigateTo("/products");

    private async Task Delete()
    {
        var confirm = await DialogService.ShowMessageBox(
            "Ürünü Sil",
            "Bu ürünü silmek istediğinize emin misiniz? Bu işlem geri alınamaz.",
            yesText: "Sil",
            cancelText: "İptal");

        if (confirm != true) return;

        var result = await ProductManager.SoftDeleteProduct(Id);
        if (result.Success)
        {
            Snackbar.Add("Ürün silindi.", Severity.Success);
            NavigationManager.NavigateTo("/products");
        }
        else
        {
            Snackbar.Add(result.Message ?? "Silme başarısız.", Severity.Error);
        }
    }

    private static string BuildVariantLabel(ProductVariantEditDetailDto pv)
    {
        var attrs = pv.VariantAttributes.Where(a => a.IsVarianter || a.IsSlicer).ToList();
        return attrs.Count > 0
            ? string.Join(" / ", attrs.Select(a => a.CategoryAttributeValueName ?? a.CustomValue ?? "?"))
            : pv.Barcode;
    }

    public class VariantPriceModel
    {
        public Guid Id { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public decimal ListPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal ECommercePrice { get; set; }
        public decimal DimensionalWeight { get; set; }
        public decimal VatRate { get; set; }
        public string CurrencyType { get; set; } = "TRY";
        public IEnumerable<EditBranchOfficeStockDto> CurrentStocks { get; set; } = [];
    }

    public class ImageEditState
    {
        public int Id { get; set; }
        public string Src { get; set; } = string.Empty;
        public bool IsMain { get; set; }
        public bool IsDeleted { get; set; }
        public Guid VariantId { get; set; }
    }
}
