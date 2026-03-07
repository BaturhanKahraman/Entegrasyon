using Microsoft.AspNetCore.Components;
using MudBlazor;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Products;

namespace Entegrasyon.Blazor.Pages.Products;

public partial class AddProduct
{
    private int stepIndex = 0;
    private MudForm? formStep1;

    private string? title;
    private string? stockCode;
    private string? description;
    private int brandId;
    private int categoryId;

    private List<Brand> brands = [];
    private List<Category> categories = [];

    private readonly List<AddProductVariantDto> variants = [];
    private List<VarianterAttributeViewModel> varianterAttributes = [];

    [Inject] private IBrandService? BrandManager { get; set; }
    [Inject] private ICategoryService? CategoryManager { get; set; }
    [Inject] private ISnackbar? Snackbar { get; set; }
    [Inject] private IProductService? ProductManager { get; set; }
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
                {
                    brands = brandsResult.Data.Select(b => new Brand { Id = b.Id, Name = b.Name }).ToList();
                }
            }
        }
        catch
        {
            // ignore; fall back to empty lists
        }
    }

    private void AddVariant()
    {
        var v = new AddProductVariantDto
        {
            Barcode = string.Empty,
            CurrencyType = "TRY",
            DimensionalWeight = 0,
            ListPrice = 0,
            SalePrice = 0,
            CostPrice = 0,
            BranchOfficeStocks = []
        };
        variants.Add(v);
    }

    private void RemoveVariant(int index)
    {
        if (index >= 0 && index < variants.Count)
            variants.RemoveAt(index);
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

        if (stepIndex < 2) stepIndex++;
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
        var dto = new AddProductDto
        {
            Title = title,
            Description = description,
            StockCode = stockCode,
            BrandId = brandId,
            CategoryId = categoryId,
            ProductVariants = variants
        };

        if (ProductManager is null)
        {
            Snackbar?.Add("Ürün yöneticisi bulunamadı.", Severity.Error);
            return;
        }

        var result = await ProductManager.AddProduct(dto);
        if (result.Success)
        {
            Snackbar?.Add("Ürün oluşturuldu.", Severity.Success);
            NavigationManager?.NavigateTo("/products");
        }
        else
        {
            Snackbar?.Add(result.Message ?? "İşlem başarısız.", Severity.Error);
        }
    }

    private async Task OnCategoryChanged(int newCategoryId)
    {
        categoryId = newCategoryId;
        await LoadVarianterAttributes();
    }

    private async Task LoadVarianterAttributes()
    {
        varianterAttributes = [];
        if (categoryId == 0)
            return;

        if (CategoryManager is null)
            return;

        var cat = await CategoryManager.GetCategoryWithAttrById(categoryId);
        if (cat is null)
            return;

        // CategoryAttributes is a junction; we need those marked as varianter
        foreach (var cac in cat.CategoryAttributes ?? [])
        {
            if (!cac.IsVarianter)
                continue;

            var attr = cac.CategoryAttribute;
            if (attr is null)
                continue;

            var values = attr.CategoryAttributeValues?.Select(v => new AttributeValueItem { Id = v.Id, Name = v.Name }).ToList() ?? [];

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
    {
        attr.SelectedValueIds = selected?.ToHashSet() ?? [];
    }

    private void GenerateVariants()
    {
        var lists = varianterAttributes
            .Where(a => a.SelectedValueIds != null && a.SelectedValueIds.Any())
            .Select(a => a.SelectedValueIds!.ToList())
            .ToList();

        if (!lists.Any())
            return;

        var total = lists.Aggregate(1, (acc, l) => acc * Math.Max(1, l.Count));
        if (total > 500)
        {
            Snackbar?.Add($"Seçilen kombinasyon sayısı çok büyük: {total}. Lütfen azaltın.", Severity.Warning);
            return;
        }

        var combos = CartesianProduct(lists);

        foreach (var combo in combos)
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
            if (depth == sequences.Count)
            {
                result.Add([.. current]);
                return;
            }

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
