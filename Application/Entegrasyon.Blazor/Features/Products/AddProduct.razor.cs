using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity;

namespace Entegrasyon.Blazor.Features.Products;

public partial class AddProduct
{
    // Steps: 0=Genel, 1=Varyant Seçimi, 2=Varyant Detayları & Görseller, 3=Pazaryeri
    private int stepIndex = 0;
    private MudForm? formStep1;
    private bool _isSaving;

    private string? title;
    private string? stockCode;
    private string? description;
    private string? season;
    private string? year;
    private int brandId;
    private int categoryId;

    private List<BrandListDetailDto> brands = [];
    private List<Category> categories = [];

    private readonly List<AddProductVariantDto> variants = [];
    private List<VarianterAttributeViewModel> varianterAttributes = [];

    // Feature 3: Regular (non-varianter, non-slicer) category attributes for Step 0
    private List<CategoryAttributeDto> _regularAttributes = [];
    private readonly Dictionary<int, int?> _regularAttrValueIds = new();
    private readonly Dictionary<int, string?> _regularAttrCustomValues = new();
    private bool _regularAttrsVisible = false;

    // Feature 4: Generated variant selection grid
    private List<GeneratedVariantRow> _generatedVariantRows = [];
    private bool _showVariantGrid = false;

    // Feature 4: Branch offices for stock entry in Step 2
    private List<BranchOffice> _branchOffices = [];
    private readonly Dictionary<(int variantIdx, int officeId), int?> _stockValues = new();

    // Images per variant index — with preview support
    private readonly Dictionary<int, List<VariantImageItem>> _variantImages = new();

    // Marketplace selection (step 3)
    private bool _trendyolSelected = true;

    [Inject] private IBrandService? BrandManager { get; set; }
    [Inject] private ICategoryService? CategoryManager { get; set; }
    [Inject] private ICategoryAttributeManager? AttributeManager { get; set; }
    [Inject] private IBranchOfficeManager? BranchOfficeManager { get; set; }
    [Inject] private ISnackbar? Snackbar { get; set; }
    [Inject] private IProductService? ProductManager { get; set; }
    [Inject] private IImageManager? ImageManager { get; set; }
    [Inject] private EventChannel<ProductCreatedForMarketplaceEvent>? MarketplaceChannel { get; set; }
    [Inject] private NavigationManager? NavigationManager { get; set; }
    [Inject] private IBarcodeService? BarcodeService { get; set; }
    [Inject] private IDialogService? DialogService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadLookupsAsync();
    }

    private async Task LoadLookupsAsync()
    {
        try
        {
            if (CategoryManager is not null)
            {
                var cats = await CategoryManager.GetSubCategories();
                if (cats?.Data is not null)
                    categories = cats.Data.Select(d => new Category { Id = d.Id, Name = d.Name }).ToList();
            }

            if (BrandManager is not null)
            {
                var brandsResult = await BrandManager.GetBrandListDetails();
                if (brandsResult?.Data is not null)
                    brands = brandsResult.Data;
            }

            if (BranchOfficeManager is not null)
            {
                var branchResult = await BranchOfficeManager.GetBranchList();
                if (branchResult?.Data is not null)
                    _branchOffices = branchResult.Data;
            }
        }
        catch
        {
            // ignore; fall back to empty lists
        }
    }

    private void RemoveVariant(int index)
    {
        if (index < 0 || index >= variants.Count) return;
        variants.RemoveAt(index);

        // Rebuild image dictionary — indices above 'index' shift down by 1
        var newImages = new Dictionary<int, List<VariantImageItem>>();
        foreach (var (key, val) in _variantImages)
        {
            if (key < index) newImages[key] = val;
            else if (key > index) newImages[key - 1] = val;
            // key == index is dropped
        }
        _variantImages.Clear();
        foreach (var (k, v) in newImages) _variantImages[k] = v;
    }

    private async Task OpenImageDialog()
    {
        if (DialogService is null) return;

        var variantInfos = variants.Select((v, i) => new ImageUploadDialog.VariantInfo
        {
            Index = i,
            Label = GetVariantLabel(v, i)
        }).ToList();

        var existingImages = _variantImages.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Select(img => new ImageUploadDialog.ImageItem
            {
                File = img.File,
                PreviewUrl = img.PreviewUrl,
                IsPrimary = img.IsPrimary
            }).ToList()
        );

        var parameters = new DialogParameters<ImageUploadDialog>
        {
            { x => x.Variants, variantInfos },
            { x => x.ExistingImages, existingImages }
        };

        var options = new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true, CloseButton = true };
        var dialog = await DialogService.Show<ImageUploadDialog>("Görsel Yönetimi", parameters, options).Result;

        if (!dialog!.Canceled && dialog.Data is Dictionary<int, List<ImageUploadDialog.ImageItem>> result)
        {
            _variantImages.Clear();
            foreach (var (idx, items) in result)
            {
                _variantImages[idx] = items.Select(item => new VariantImageItem
                {
                    File = item.File,
                    PreviewUrl = item.PreviewUrl,
                    IsPrimary = item.IsPrimary
                }).ToList();
            }
        }
    }

    private async Task GenerateBarcodeForVariant(int idx)
    {
        if (BarcodeService is null || idx >= variants.Count) return;
        variants[idx].Barcode = await BarcodeService.GenerateAsync();
    }


    private static string GetVariantLabel(AddProductVariantDto variant, int idx)
    {
        if (variant.ProductVariantAttributes?.Count > 0)
            return string.Join(" / ", variant.ProductVariantAttributes
                .Where(a => a.IsVarianter || a.IsSlicer)
                .Select(a => a.CategoryAttributeValue ?? a.CustomValue ?? "?"));
        return $"Varyant {idx + 1}";
    }

    private async Task NextStep()
    {
        if (stepIndex == 0)
        {
            if (formStep1 is not null)
                await formStep1.Validate();
            if (formStep1?.IsValid != true)
            {
                Snackbar?.Add("Lütfen gerekli alanları doldurun.", Severity.Warning);
                return;
            }
            // Ensure attributes are loaded if user never triggered a change event
            if (varianterAttributes.Count == 0 && _regularAttributes.Count == 0 && categoryId != 0)
                await LoadVarianterAttributes();

            // Validate required regular attributes
            var missingRequired = _regularAttributes
                .Where(a => a.IsRequired)
                .Where(a => !(_regularAttrValueIds.TryGetValue(a.Id, out var vid) && vid.HasValue) &&
                            !(_regularAttrCustomValues.TryGetValue(a.Id, out var cv) && !string.IsNullOrWhiteSpace(cv)))
                .Select(a => a.CategoriyAttributeHumanized)
                .ToList();
            if (missingRequired.Count > 0)
            {
                Snackbar?.Add($"Zorunlu özellikler eksik: {string.Join(", ", missingRequired)}", Severity.Warning);
                return;
            }
        }

        if (stepIndex == 1 && varianterAttributes.Count > 0 && variants.Count == 0)
        {
            Snackbar?.Add("Lütfen önce varyant seçimini onaylayın.", Severity.Warning);
            return;
        }

        if (stepIndex < 3) stepIndex++;
    }

    private void PreviousStep()
    {
        if (stepIndex > 0) stepIndex--;
    }

    private async Task Submit()
    {
        if (string.IsNullOrWhiteSpace(title) || brandId == 0 || categoryId == 0)
        {
            Snackbar?.Add("Lütfen başlık, marka ve kategori seçin.", Severity.Warning);
            return;
        }

        if (ProductManager is null || ImageManager is null)
        {
            Snackbar?.Add("Servis bulunamadı.", Severity.Error);
            return;
        }

        // Map stock values into each variant's BranchOfficeStocks
        for (int vi = 0; vi < variants.Count; vi++)
        {
            variants[vi].BranchOfficeStocks = _branchOffices
                .Select(o => new AddBranchOfficeStockDto
                {
                    BranchOfficeId = o.Id,
                    FirstTotalStock = _stockValues.GetValueOrDefault((vi, o.Id)) ?? 0
                })
                .ToList();
        }

        // Map regular attributes to AttributeKeyValues
        var attributeKeyValues = _regularAttributes
            .Select(a => new AttributeKeyValue
            {
                CategoryAttributeId = a.Id,
                AttributeValueId = _regularAttrValueIds.TryGetValue(a.Id, out var vid) ? vid : null,
                CustomValue = _regularAttrCustomValues.TryGetValue(a.Id, out var cv) ? cv : null
            })
            .Where(akv => akv.AttributeValueId.HasValue || !string.IsNullOrWhiteSpace(akv.CustomValue))
            .ToList();

        _isSaving = true;
        try
        {
            // 1. Save product to DB
            var dto = new AddProductDto
            {
                Title = title,
                Description = description,
                StockCode = stockCode,
                Season = season,
                Year = year,
                BrandId = brandId,
                CategoryId = categoryId,
                AttributeKeyValues = attributeKeyValues,
                ProductVariants = variants
            };

            var result = await ProductManager.AddProduct(dto);
            if (!result.Success)
            {
                Snackbar?.Add(result.Message ?? "Ürün oluşturulamadı.", Severity.Error);
                return;
            }

            var product = result.Data;

            // 2. Upload images (synchronous — Trendyol needs the URLs)
            var imageUploads = BuildImageUploads(product);
            if (imageUploads.Count > 0)
                await ImageManager.AddProductImages(product.Id, imageUploads);

            // 3. Queue marketplace sync (fire & forget)
            var selectedMarketplaces = GetSelectedMarketplaces();
            if (selectedMarketplaces.Count > 0 && MarketplaceChannel is not null)
            {
                MarketplaceChannel.TryPublish(new ProductCreatedForMarketplaceEvent(product.Id, selectedMarketplaces));
            }

            Snackbar?.Add("Ürün oluşturuldu. Pazaryeri senkronizasyonu arka planda başlatıldı.", Severity.Success);
            NavigationManager?.NavigateTo("/products");
        }
        catch (FluentValidation.ValidationException vex)
        {
            Snackbar?.Add(string.Join(" | ", vex.Errors.Select(e => e.ErrorMessage)), Severity.Warning);
        }
        catch (Exception ex)
        {
            Snackbar?.Add($"Beklenmeyen hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSaving = false;
        }
    }

    private List<VariantImageStream> BuildImageUploads(Product product)
    {
        var result = new List<VariantImageStream>();
        var savedVariants = product.ProductVariants.ToList();

        foreach (var (variantIdx, items) in _variantImages)
        {
            if (variantIdx >= variants.Count || variantIdx >= savedVariants.Count)
                continue;

            var variantBarcode = variants[variantIdx].Barcode;
            var savedVariant = savedVariants.FirstOrDefault(v => v.Barcode == variantBarcode);

            if (savedVariant is null) continue;

            foreach (var item in items)
            {
                if (item.File is null) continue;
                result.Add(new VariantImageStream(
                    savedVariant.Id,
                    item.File.OpenReadStream(maxAllowedSize: 10_000_000),
                    item.File.Name,
                    item.IsPrimary));
            }
        }
        return result;
    }

    private List<string> GetSelectedMarketplaces()
    {
        var list = new List<string>();
        if (_trendyolSelected) list.Add("Trendyol");
        return list;
    }

    private async Task OnCategoryChanged(int newCategoryId)
    {
        categoryId = newCategoryId;
        variants.Clear();
        _variantImages.Clear();
        _generatedVariantRows.Clear();
        _showVariantGrid = false;
        _regularAttributes = [];
        _regularAttrValueIds.Clear();
        _regularAttrCustomValues.Clear();
        _regularAttrsVisible = false;
        _stockValues.Clear();
        StateHasChanged();
        await LoadVarianterAttributes();
    }

    private async Task LoadVarianterAttributes()
    {
        varianterAttributes = [];
        _regularAttributes = [];
        if (categoryId == 0 || AttributeManager is null) return;

        var result = await AttributeManager.GetCategoryAttributesByCategory(categoryId);
        if (!result.Success || result.Data is null) return;

        foreach (var attr in result.Data)
        {
            if (attr.IsVarianter || attr.IsSlicer)
            {
                var values = attr.CategoryAttributeValues
                    .Select(v => new AttributeValueItem { Id = v.Id, Name = v.Name }).ToList();
                varianterAttributes.Add(new VarianterAttributeViewModel
                {
                    AttributeId = attr.Id,
                    AttributeName = attr.CategoriyAttributeHumanized ?? attr.CategoryAttributeKey,
                    IsVarianter = attr.IsVarianter,
                    IsSlicer = attr.IsSlicer,
                    AllowCustom = attr.AllowCustom,
                    Values = values,
                    SelectedValueIds = []
                });
            }
            else
            {
                _regularAttributes.Add(attr);
            }
        }

        _regularAttrsVisible = _regularAttributes.Count > 0;

        // Kategori varyant özelliği içermiyorsa tek boş varyant otomatik ekle
        if (varianterAttributes.Count == 0)
            variants.Add(new AddProductVariantDto { Barcode = string.Empty, CurrencyType = "TRY", BranchOfficeStocks = [] });
    }

    private void OnSelectedValuesChanged(VarianterAttributeViewModel attr, IEnumerable<int> selected)
        => attr.SelectedValueIds = selected?.ToHashSet() ?? [];

    private void GenerateVariants()
    {
        // Her attr için (valueId?, valueName) listesi oluştur
        var selections = new List<(VarianterAttributeViewModel Attr, List<(int? ValueId, string ValueName)> Values)>();

        foreach (var attr in varianterAttributes)
        {
            if (attr.AllowCustom)
            {
                var customVals = attr.CustomValueText
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => ((int?)null, v))
                    .ToList();
                if (customVals.Count > 0)
                    selections.Add((attr, customVals));
            }
            else
            {
                if (attr.SelectedValueIds == null || attr.SelectedValueIds.Count == 0) continue;
                var vals = attr.SelectedValueIds
                    .Select(id => ((int?)id, attr.Values.FirstOrDefault(v => v.Id == id)?.Name ?? id.ToString()))
                    .ToList();
                selections.Add((attr, vals));
            }
        }

        if (selections.Count == 0) return;

        var total = selections.Aggregate(1, (acc, s) => acc * s.Values.Count);
        if (total > 500)
        {
            Snackbar?.Add($"Seçilen kombinasyon sayısı çok büyük: {total}. Lütfen azaltın.", Severity.Warning);
            return;
        }

        _generatedVariantRows.Clear();

        foreach (var combo in CartesianProduct(selections.Select(s => s.Values).ToList()))
        {
            var variant = new AddProductVariantDto
            {
                Barcode = string.Empty,
                CurrencyType = "TRY",
                ProductVariantAttributes = [],
                BranchOfficeStocks = []
            };
            for (int i = 0; i < combo.Count; i++)
            {
                var (valueId, valueName) = combo[i];
                var attrVm = selections[i].Attr;
                variant.ProductVariantAttributes.Add(new ProductVariantAttribute
                {
                    CategoryAttributeValueId = valueId,
                    CategoryAttributeValue = valueId.HasValue ? valueName : null,
                    CustomValue = valueId.HasValue ? null : valueName,
                    IsVarianter = attrVm.IsVarianter,
                    IsSlicer = attrVm.IsSlicer
                });
            }
            _generatedVariantRows.Add(new GeneratedVariantRow
            {
                IsSelected = true,
                Label = GetVariantLabel(variant, _generatedVariantRows.Count),
                Variant = variant
            });
        }

        _showVariantGrid = true;
    }

    private void ConfirmVariantSelection()
    {
        variants.Clear();
        _variantImages.Clear();
        _stockValues.Clear();
        foreach (var row in _generatedVariantRows.Where(r => r.IsSelected))
            variants.Add(row.Variant);
        _showVariantGrid = false;
    }

    private static List<List<(int?, string)>> CartesianProduct(List<List<(int?, string)>> sequences)
    {
        var result = new List<List<(int?, string)>>();
        void Recurse(int depth, List<(int?, string)> current)
        {
            if (depth == sequences.Count) { result.Add([.. current]); return; }
            foreach (var item in sequences[depth])
            {
                current.Add(item);
                Recurse(depth + 1, current);
                current.RemoveAt(current.Count - 1);
            }
        }
        Recurse(0, []);
        return result;
    }

    private class VariantImageItem
    {
        public IBrowserFile? File { get; set; }
        public string PreviewUrl { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
    }

    private class VarianterAttributeViewModel
    {
        public int AttributeId { get; set; }
        public string? AttributeName { get; set; }
        public bool IsVarianter { get; set; }
        public bool IsSlicer { get; set; }
        public bool AllowCustom { get; set; }
        public string CustomValueText { get; set; } = string.Empty;
        public List<AttributeValueItem> Values { get; set; } = [];
        public HashSet<int>? SelectedValueIds { get; set; } = [];
    }

    private class AttributeValueItem
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private class GeneratedVariantRow
    {
        public bool IsSelected { get; set; } = true;
        public string Label { get; set; } = string.Empty;
        public AddProductVariantDto Variant { get; set; } = null!;
    }

    private int? GetStock(int variantIdx, int officeId)
        => _stockValues.GetValueOrDefault((variantIdx, officeId));

    private void SetStock(int variantIdx, int officeId, int? value)
        => _stockValues[(variantIdx, officeId)] = value;
}
