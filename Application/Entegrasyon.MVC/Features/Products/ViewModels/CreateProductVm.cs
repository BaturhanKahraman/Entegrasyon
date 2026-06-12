namespace Entegrasyon.MVC.Features.Products.ViewModels;

public class CreateProductVm
{
    // Step 1: General
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? StockCode { get; set; }
    public string? Season { get; set; }
    public string? Year { get; set; }
    public int BrandId { get; set; }
    public int CategoryId { get; set; }
    public string? BrandName { get; set; }
    public string? CategoryName { get; set; }

    // Step 2: Category Attributes (non-varianter, non-slicer)
    public List<AttributeValueVm> CategoryAttributes { get; set; } = [];

    // Step 3: Variant Generation
    public List<VariantAttributeSelectionVm> VariantAttributeSelections { get; set; } = [];
    public DefaultVariantValuesVm DefaultValues { get; set; } = new();
    public List<CreateVariantVm> Variants { get; set; } = [];

    // Step 4: Image assignments (temp file keys from upload)
    public List<VariantImageAssignmentVm> ImageAssignments { get; set; } = [];

    // Step 6: Storefront Publish (optional)
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? SeoSlug { get; set; }
    public string? SeoKeywords { get; set; }

    public static List<CreateVariantVm> GenerateVariants(
        List<VariantAttributeSelectionVm> selections,
        DefaultVariantValuesVm defaults)
    {
        var nonEmpty = selections.Where(s => s.SelectedValues.Count > 0).ToList();
        if (nonEmpty.Count == 0)
        {
            // Kategoride hiç varyant özelliği yoksa (selections boş) tek varsayılan varyant üret —
            // kullanıcı barkod/fiyat/stok girip devam edebilsin. Özellik VAR ama seçilmemişse boş dön.
            if (selections.Count == 0)
            {
                return
                [
                    new CreateVariantVm
                    {
                        VariantAttributes = [],
                        ListPrice = defaults.ListPrice,
                        SalePrice = defaults.SalePrice,
                        CostPrice = defaults.CostPrice,
                        VatRate = defaults.VatRate
                    }
                ];
            }
            return [];
        }

        IEnumerable<List<VariantAttributeValueVm>> combos = nonEmpty[0].SelectedValues
            .Select(v => new List<VariantAttributeValueVm>
            {
                new()
                {
                    CategoryAttributeId = nonEmpty[0].CategoryAttributeId,
                    AttributeName = nonEmpty[0].AttributeName,
                    ValueId = v.ValueId,
                    ValueName = v.ValueName,
                    IsCustom = v.IsCustom,
                    IsVarianter = nonEmpty[0].IsVarianter,
                    IsSlicer = nonEmpty[0].IsSlicer
                }
            });

        for (int i = 1; i < nonEmpty.Count; i++)
        {
            var attr = nonEmpty[i];
            combos = combos.SelectMany(existing =>
                attr.SelectedValues.Select(v =>
                    existing.Concat([new VariantAttributeValueVm
                    {
                        CategoryAttributeId = attr.CategoryAttributeId,
                        AttributeName = attr.AttributeName,
                        ValueId = v.ValueId,
                        ValueName = v.ValueName,
                        IsCustom = v.IsCustom,
                        IsVarianter = attr.IsVarianter,
                        IsSlicer = attr.IsSlicer
                    }]).ToList()));
        }

        return combos.Select(attrs => new CreateVariantVm
        {
            VariantAttributes = attrs,
            ListPrice = defaults.ListPrice,
            SalePrice = defaults.SalePrice,
            CostPrice = defaults.CostPrice,
            VatRate = defaults.VatRate
        }).ToList();
    }
}

public class CreateVariantVm
{
    public string Barcode { get; set; } = "";
    public string? Name { get; set; }
    public decimal? ListPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal VatRate { get; set; } = 20;
    public decimal DimensionalWeight { get; set; }
    public decimal ECommercePrice { get; set; }
    public List<BranchOfficeStockVm> BranchOfficeStocks { get; set; } = [];
    public List<VariantAttributeValueVm> VariantAttributes { get; set; } = [];
}

public class BranchOfficeStockVm
{
    public int BranchOfficeId { get; set; }
    public string BranchOfficeName { get; set; } = "";
    public int Stock { get; set; }
}

public class VariantAttributeValueVm
{
    public int CategoryAttributeId { get; set; }
    public string AttributeName { get; set; } = "";
    public int? ValueId { get; set; }
    public string ValueName { get; set; } = "";
    public bool IsCustom { get; set; }
    public bool IsVarianter { get; set; }
    public bool IsSlicer { get; set; }
}

public class VariantAttributeSelectionVm
{
    public int CategoryAttributeId { get; set; }
    public string AttributeName { get; set; } = "";
    public bool IsVarianter { get; set; }
    public bool IsSlicer { get; set; }
    public List<SelectedAttributeValueVm> SelectedValues { get; set; } = [];
}

public class SelectedAttributeValueVm
{
    public int? ValueId { get; set; }
    public string ValueName { get; set; } = "";
    public bool IsCustom { get; set; }
}

public class DefaultVariantValuesVm
{
    public decimal? ListPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal VatRate { get; set; } = 20;
}

public class AttributeValueVm
{
    public int CategoryAttributeId { get; set; }
    public string AttributeName { get; set; } = "";
    public int? ValueId { get; set; }
    public string? CustomValue { get; set; }
    public bool IsRequired { get; set; }
}

public class VariantImageAssignmentVm
{
    public int VariantIndex { get; set; }
    public List<string> TempImageKeys { get; set; } = [];
    public int? MainImageIndex { get; set; }
}
