using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events;
using Entegrasyon.Entity.Products;
using BrandEntity = Entegrasyon.Entity.Brands.Brand;

namespace Entegrasyon.Blazor.Pages.Products;

public partial class AddProduct
{
    // Steps: 0=Genel, 1=Varyant Seçimi, 2=Varyant Detayları & Görseller, 3=Pazaryeri
    private int stepIndex = 0;
    private MudForm? formStep1;
    private bool _isSaving;

    private string? title;
    private string? stockCode;
    private string? description;
    private int brandId;
    private int categoryId;

    private List<BrandEntity> brands = [];
    private List<Category> categories = [];

    private readonly List<AddProductVariantDto> variants = [];
    private List<VarianterAttributeViewModel> varianterAttributes = [];

    // Images per variant index
    private readonly Dictionary<int, List<IBrowserFile>> _variantImages = new();

    // Marketplace selection (step 3)
    private bool _trendyolSelected = true;

    [Inject] private IBrandService? BrandManager { get; set; }
    [Inject] private ICategoryService? CategoryManager { get; set; }
    [Inject] private ISnackbar? Snackbar { get; set; }
    [Inject] private IProductService? ProductManager { get; set; }
    [Inject] private IImageManager? ImageManager { get; set; }
    [Inject] private EventChannel<ProductCreatedForMarketplaceEvent>? MarketplaceChannel { get; set; }
    [Inject] private NavigationManager? NavigationManager { get; set; }

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
                var cats = await CategoryManager.GetAllCategoriesWithoutAttributesAsync();
                if (cats is not null)
                    categories = cats;
            }

            if (BrandManager is not null)
            {
                var brandsResult = await BrandManager.GetBrandListDetails();
                if (brandsResult?.Data is not null)
                    brands = brandsResult.Data.Select(b => new BrandEntity { Id = b.Id, Name = b.Name }).ToList();
            }
        }
        catch
        {
            // ignore; fall back to empty lists
        }
    }

    private void AddVariant()
    {
        variants.Add(new AddProductVariantDto
        {
            Barcode = string.Empty,
            CurrencyType = "TRY",
            DimensionalWeight = 0,
            ListPrice = 0,
            SalePrice = 0,
            CostPrice = 0,
            BranchOfficeStocks = []
        });
    }

    private void RemoveVariant(int index)
    {
        if (index >= 0 && index < variants.Count)
        {
            variants.RemoveAt(index);
            _variantImages.Remove(index);
        }
    }

    private void OnVariantImagesChanged(int variantIdx, IReadOnlyList<IBrowserFile> files)
    {
        _variantImages[variantIdx] = [..files];
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

        _isSaving = true;
        try
        {
            // 1. Save product to DB
            var dto = new AddProductDto
            {
                Title = title,
                Description = description,
                StockCode = stockCode,
                BrandId = brandId,
                CategoryId = categoryId,
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
        finally
        {
            _isSaving = false;
        }
    }

    private List<VariantImageStream> BuildImageUploads(Product product)
    {
        var result = new List<VariantImageStream>();
        var savedVariants = product.ProductVariants.ToList();

        foreach (var (variantIdx, files) in _variantImages)
        {
            if (variantIdx >= variants.Count || variantIdx >= savedVariants.Count)
                continue;

            var variantBarcode = variants[variantIdx].Barcode;
            var savedVariant = savedVariants.FirstOrDefault(v => v.Barcode == variantBarcode);

            if (savedVariant is null) continue;

            bool isFirst = true;
            foreach (var file in files)
            {
                result.Add(new VariantImageStream(
                    savedVariant.Id,
                    file.OpenReadStream(maxAllowedSize: 10_000_000),
                    file.Name,
                    isFirst));
                isFirst = false;
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
        await LoadVarianterAttributes();
    }

    private async Task LoadVarianterAttributes()
    {
        varianterAttributes = [];
        if (categoryId == 0 || CategoryManager is null) return;

        var cat = await CategoryManager.GetCategoryWithAttrById(categoryId);
        if (cat is null) return;

        foreach (var cac in cat.CategoryAttributes ?? [])
        {
            if (!cac.IsVarianter) continue;
            var attr = cac.CategoryAttribute;
            if (attr is null) continue;
            var values = attr.CategoryAttributeValues?
                .Select(v => new AttributeValueItem { Id = v.Id, Name = v.Name }).ToList() ?? [];
            varianterAttributes.Add(new VarianterAttributeViewModel
            {
                AttributeId = attr.Id,
                AttributeName = attr.CategoryAttributeHumanized ?? attr.CategoryAttributeKey,
                Values = values,
                SelectedValueIds = []
            });
        }
    }

    private void OnSelectedValuesChanged(VarianterAttributeViewModel attr, IEnumerable<int> selected)
        => attr.SelectedValueIds = selected?.ToHashSet() ?? [];

    private void GenerateVariants()
    {
        var lists = varianterAttributes
            .Where(a => a.SelectedValueIds != null && a.SelectedValueIds.Any())
            .Select(a => a.SelectedValueIds!.ToList())
            .ToList();

        if (!lists.Any()) return;

        var total = lists.Aggregate(1, (acc, l) => acc * Math.Max(1, l.Count));
        if (total > 500)
        {
            Snackbar?.Add($"Seçilen kombinasyon sayısı çok büyük: {total}. Lütfen azaltın.", Severity.Warning);
            return;
        }

        foreach (var combo in CartesianProduct(lists))
        {
            var variant = new AddProductVariantDto
            {
                Barcode = string.Empty,
                CurrencyType = "TRY",
                DimensionalWeight = 0,
                ListPrice = 0,
                SalePrice = 0,
                CostPrice = 0,
                ProductVariantAttributes = [],
                BranchOfficeStocks = []
            };
            for (int i = 0; i < combo.Count; i++)
            {
                var attrVm = varianterAttributes.ElementAt(i);
                var valueId = combo[i];
                var value = attrVm.Values.FirstOrDefault(v => v.Id == valueId);
                variant.ProductVariantAttributes.Add(new ProductVariantAttribute
                {
                    CategoryAttributeValueId = valueId,
                    CategoryAttributeValue = value?.Name,
                    IsVarianter = true
                });
            }
            variants.Add(variant);
        }
    }

    private static List<List<int>> CartesianProduct(List<List<int>> sequences)
    {
        var result = new List<List<int>>();
        void Recurse(int depth, List<int> current)
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

    private class VarianterAttributeViewModel
    {
        public int AttributeId { get; set; }
        public string? AttributeName { get; set; }
        public List<AttributeValueItem> Values { get; set; } = [];
        public HashSet<int>? SelectedValueIds { get; set; } = [];
    }

    private class AttributeValueItem
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
