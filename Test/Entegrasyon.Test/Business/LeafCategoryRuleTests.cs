using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.ProductVariant;
using FluentAssertions;

namespace Entegrasyon.Test.Business;

public class LeafCategoryRuleTests
{
    [Fact]
    public async Task AddProduct_WithZeroCategoryId_FailsValidation()
    {
        var validator = new Entegrasyon.Business.Validation.FluentValidation.AddProductValidator();
        var dto = new AddProductDto
        {
            Title = "Test Product",
            CategoryId = 0,
            BrandId = 1,
            ProductVariants = [new AddProductVariantDto
            {
                Barcode = "123", ListPrice = 100, SalePrice = 90, CurrencyType = "TRY",
                BranchOfficeStocks = [new AddBranchOfficeStockDto { BranchOfficeId = 1, FirstTotalStock = 10 }]
            }]
        };

        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CategoryId");
    }

    [Fact]
    public async Task AddProduct_WithOptionalDescriptionAndStockCode_IsValid()
    {
        var validator = new Entegrasyon.Business.Validation.FluentValidation.AddProductValidator();
        var dto = new AddProductDto
        {
            Title = "Test Product",
            Description = "",
            StockCode = "",
            CategoryId = 5,
            BrandId = 1,
            ProductVariants = [new AddProductVariantDto
            {
                Barcode = "123", ListPrice = 100, SalePrice = 90, CurrencyType = "TRY",
                BranchOfficeStocks = [new AddBranchOfficeStockDto { BranchOfficeId = 1, FirstTotalStock = 10 }]
            }]
        };

        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();
    }
}
