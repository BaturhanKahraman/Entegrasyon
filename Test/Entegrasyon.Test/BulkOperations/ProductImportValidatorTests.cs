using Entegrasyon.Business.Concrete.BulkOperations;
using Entegrasyon.Entity.Dtos.BulkOperations;

namespace Entegrasyon.UnitTest.BulkOperations;

public class ProductImportValidatorTests
{
    private readonly ProductImportValidator _validator = new();

    #region Product Row Validation

    [Fact]
    public void ValidateProductRows_EmptyBarcode_ReturnsError()
    {
        var rows = new List<ProductImportRow>
        {
            new(2, "", "Test", null, 100m, 90m, 50m, 20m, null, null)
        };

        var errors = _validator.ValidateProductRows(rows);

        errors.Should().ContainSingle(e => e.ErrorMessage.Contains("Barkod boş"));
    }

    [Fact]
    public void ValidateProductRows_EmptyTitle_ReturnsError()
    {
        var rows = new List<ProductImportRow>
        {
            new(2, "1234567890", "", null, 100m, 90m, 50m, 20m, null, null)
        };

        var errors = _validator.ValidateProductRows(rows);

        errors.Should().ContainSingle(e => e.ErrorMessage.Contains("Ürün adı boş"));
    }

    [Fact]
    public void ValidateProductRows_NegativeListPrice_ReturnsError()
    {
        var rows = new List<ProductImportRow>
        {
            new(2, "1234567890", "Test", null, -10m, 90m, 50m, 20m, null, null)
        };

        var errors = _validator.ValidateProductRows(rows);

        errors.Should().ContainSingle(e => e.ErrorMessage.Contains("Liste fiyatı negatif"));
    }

    [Fact]
    public void ValidateProductRows_NegativeSalePrice_ReturnsError()
    {
        var rows = new List<ProductImportRow>
        {
            new(2, "1234567890", "Test", null, 100m, -5m, 50m, 20m, null, null)
        };

        var errors = _validator.ValidateProductRows(rows);

        errors.Should().ContainSingle(e => e.ErrorMessage.Contains("Satış fiyatı negatif"));
    }

    [Fact]
    public void ValidateProductRows_SalePriceExceedsListPrice_ReturnsError()
    {
        var rows = new List<ProductImportRow>
        {
            new(2, "1234567890", "Test", null, 100m, 150m, 50m, 20m, null, null)
        };

        var errors = _validator.ValidateProductRows(rows);

        errors.Should().ContainSingle(e => e.ErrorMessage.Contains("Satış fiyatı liste fiyatından büyük"));
    }

    [Fact]
    public void ValidateProductRows_InvalidVatRate_ReturnsError()
    {
        var rows = new List<ProductImportRow>
        {
            new(2, "1234567890", "Test", null, 100m, 90m, 50m, 25m, null, null)
        };

        var errors = _validator.ValidateProductRows(rows);

        errors.Should().ContainSingle(e => e.ErrorMessage.Contains("KDV oranı"));
    }

    [Fact]
    public void ValidateProductRows_ZeroPrices_ReturnsError()
    {
        var rows = new List<ProductImportRow>
        {
            new(2, "1234567890", "Test", null, 0m, 0m, 0m, 20m, null, null)
        };

        var errors = _validator.ValidateProductRows(rows);

        errors.Should().Contain(e => e.ErrorMessage.Contains("Liste fiyatı sıfırdan büyük olmalı"));
    }

    [Fact]
    public void ValidateProductRows_ValidRow_ReturnsNoErrors()
    {
        var rows = new List<ProductImportRow>
        {
            new(2, "1234567890", "Test Ürün", "STK-001", 100m, 90m, 50m, 20m, "Giyim", "Nike")
        };

        var errors = _validator.ValidateProductRows(rows);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateProductRows_BarcodeTooLong_ReturnsError()
    {
        var rows = new List<ProductImportRow>
        {
            new(2, new string('1', 51), "Test", null, 100m, 90m, 50m, 20m, null, null)
        };

        var errors = _validator.ValidateProductRows(rows);

        errors.Should().ContainSingle(e => e.ErrorMessage.Contains("Barkod 50 karakterden uzun"));
    }

    [Fact]
    public void ValidateProductRows_TitleTooLong_ReturnsError()
    {
        var rows = new List<ProductImportRow>
        {
            new(2, "1234567890", new string('A', 501), null, 100m, 90m, 50m, 20m, null, null)
        };

        var errors = _validator.ValidateProductRows(rows);

        errors.Should().ContainSingle(e => e.ErrorMessage.Contains("Ürün adı 500 karakterden uzun"));
    }

    #endregion

    #region Price Row Validation

    [Fact]
    public void ValidatePriceRows_SalePriceExceedsListPrice_ReturnsError()
    {
        var rows = new List<PriceImportRow>
        {
            new(2, "1234567890", 100m, 150m, 50m)
        };

        var errors = _validator.ValidatePriceRows(rows);

        errors.Should().ContainSingle(e => e.ErrorMessage.Contains("Satış fiyatı liste fiyatından büyük"));
    }

    [Fact]
    public void ValidatePriceRows_ZeroListPrice_ReturnsError()
    {
        var rows = new List<PriceImportRow>
        {
            new(2, "1234567890", 0m, 0m, 0m)
        };

        var errors = _validator.ValidatePriceRows(rows);

        errors.Should().Contain(e => e.ErrorMessage.Contains("Liste fiyatı sıfırdan büyük olmalı"));
    }

    [Fact]
    public void ValidatePriceRows_ValidRow_ReturnsNoErrors()
    {
        var rows = new List<PriceImportRow>
        {
            new(2, "1234567890", 100m, 90m, 50m)
        };

        var errors = _validator.ValidatePriceRows(rows);

        errors.Should().BeEmpty();
    }

    #endregion

    #region Stock Row Validation

    [Fact]
    public void ValidateStockRows_NegativeQuantity_ReturnsError()
    {
        var rows = new List<StockImportRow>
        {
            new(2, "1234567890", 1, -5)
        };

        var errors = _validator.ValidateStockRows(rows);

        errors.Should().ContainSingle(e => e.ErrorMessage.Contains("Stok miktarı negatif"));
    }

    [Fact]
    public void ValidateStockRows_InvalidBranchOffice_ReturnsError()
    {
        var rows = new List<StockImportRow>
        {
            new(2, "1234567890", 0, 50)
        };

        var errors = _validator.ValidateStockRows(rows);

        errors.Should().ContainSingle(e => e.ErrorMessage.Contains("Şube ID geçersiz"));
    }

    [Fact]
    public void ValidateStockRows_ValidRow_ReturnsNoErrors()
    {
        var rows = new List<StockImportRow>
        {
            new(2, "1234567890", 1, 50)
        };

        var errors = _validator.ValidateStockRows(rows);

        errors.Should().BeEmpty();
    }

    #endregion
}
