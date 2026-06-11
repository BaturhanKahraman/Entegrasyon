// File: Test/Entegrasyon.Test/MVC/ProductWizardViewModelTests.cs
using Entegrasyon.MVC.Features.Products.ViewModels;
using FluentAssertions;

namespace Entegrasyon.Test.MVC;

public class ProductWizardViewModelTests
{
    [Fact]
    public void GenerateVariants_TwoAttributes_ReturnsCartesianProduct()
    {
        var selections = new List<VariantAttributeSelectionVm>
        {
            new()
            {
                CategoryAttributeId = 1, AttributeName = "Beden",
                IsVarianter = true, IsSlicer = false, AllowCustom = false,
                SelectedValues =
                [
                    new SelectedAttributeValueVm { ValueId = 10, ValueName = "S" },
                    new SelectedAttributeValueVm { ValueId = 11, ValueName = "M" },
                    new SelectedAttributeValueVm { ValueId = 12, ValueName = "L" }
                ]
            },
            new()
            {
                CategoryAttributeId = 2, AttributeName = "Renk",
                IsVarianter = false, IsSlicer = true, AllowCustom = true,
                SelectedValues =
                [
                    new SelectedAttributeValueVm { ValueId = null, ValueName = "Kirmizi", IsCustom = true },
                    new SelectedAttributeValueVm { ValueId = null, ValueName = "Mavi", IsCustom = true }
                ]
            }
        };

        var defaults = new DefaultVariantValuesVm
        { ListPrice = 299.90m, SalePrice = 249.90m, CostPrice = 120m, VatRate = 20 };

        var variants = CreateProductVm.GenerateVariants(selections, defaults);

        variants.Should().HaveCount(6);
        variants[0].VariantAttributes.Should().HaveCount(2);
        variants[0].ListPrice.Should().Be(299.90m);
        variants[0].BranchOfficeStocks.Should().BeEmpty();
        var combos = variants.Select(v => string.Join("-", v.VariantAttributes.Select(a => a.ValueName))).ToList();
        combos.Should().Contain("S-Kirmizi");
        combos.Should().Contain("M-Mavi");
        combos.Should().Contain("L-Kirmizi");
    }

    [Fact]
    public void GenerateVariants_SingleAttribute_ReturnsOnePerValue()
    {
        var selections = new List<VariantAttributeSelectionVm>
        {
            new()
            {
                CategoryAttributeId = 1, AttributeName = "Beden",
                IsVarianter = true, IsSlicer = false, AllowCustom = false,
                SelectedValues =
                [
                    new SelectedAttributeValueVm { ValueId = 10, ValueName = "S" },
                    new SelectedAttributeValueVm { ValueId = 11, ValueName = "M" }
                ]
            }
        };

        var defaults = new DefaultVariantValuesVm { ListPrice = 100, SalePrice = 90, CostPrice = 50, VatRate = 20 };
        var variants = CreateProductVm.GenerateVariants(selections, defaults);

        variants.Should().HaveCount(2);
        variants[0].VariantAttributes.Should().ContainSingle(a => a.ValueName == "S");
        variants[1].VariantAttributes.Should().ContainSingle(a => a.ValueName == "M");
    }

    [Fact]
    public void GenerateVariants_NoVariantAttributesInCategory_ReturnsSingleDefaultVariant()
    {
        // Kategoride hiç varyant özelliği yok (selections boş) → Option A:
        // tek varsayılan varyant üretilir, kullanıcı barkod/fiyat/stok girip devam eder.
        var defaults = new DefaultVariantValuesVm
        { ListPrice = 199.90m, SalePrice = 149.90m, CostPrice = 80m, VatRate = 18 };

        var variants = CreateProductVm.GenerateVariants([], defaults);

        variants.Should().ContainSingle();
        variants[0].VariantAttributes.Should().BeEmpty();
        variants[0].ListPrice.Should().Be(199.90m);
        variants[0].SalePrice.Should().Be(149.90m);
        variants[0].CostPrice.Should().Be(80m);
        variants[0].VatRate.Should().Be(18);
    }

    [Fact]
    public void GenerateVariants_AttributesExistButNoValuesSelected_ReturnsEmpty()
    {
        // Kategoride varyant özelliği VAR ama hiçbir değer seçilmedi → boş dön (seçim zorunlu).
        var selections = new List<VariantAttributeSelectionVm>
        {
            new()
            {
                CategoryAttributeId = 1, AttributeName = "Beden",
                IsVarianter = true, IsSlicer = false, AllowCustom = false,
                SelectedValues = []
            }
        };

        var variants = CreateProductVm.GenerateVariants(selections, new DefaultVariantValuesVm());

        variants.Should().BeEmpty();
    }
}
