using ClosedXML.Excel;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.BulkOperations;
using Entegrasyon.Entity.BulkOperations;
using Entegrasyon.Entity.Dtos.BulkOperations;
using Entegrasyon.Entity.Products;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.UnitTest.BulkOperations;

public class ValidateImportTests : BaseTest
{
    private readonly ExcelParser _excelParser = new();
    private readonly ProductImportValidator _importValidator = new();
    private readonly Mock<ILogger<BulkOperationManager>> _mockLogger = new();
    private readonly IBulkOperationManager _manager;

    public ValidateImportTests()
    {
        mockIntegrationDbContext
            .Setup(x => x.BulkOperationLogs)
            .ReturnsDbSet(new List<BulkOperationLog>());

        mockIntegrationDbContext
            .Setup(x => x.ProductVariants)
            .ReturnsDbSet(new List<ProductVariant>());

        mockIntegrationDbContext
            .Setup(x => x.BranchOfficeStocks)
            .ReturnsDbSet(new List<BranchOfficeStock>());

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _manager = new BulkOperationManager(
            mockContextFactory.Object,
            _excelParser,
            new CsvParser(),
            _importValidator,
            mockApplicationLogger.Object,
            _mockLogger.Object);
    }

    private static Stream CreateProductExcel(List<object[]>? rows = null)
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");

        var headers = new[] { "Barkod", "Ürün Adı", "Stok Kodu", "Liste Fiyatı", "Satış Fiyatı", "Maliyet Fiyatı", "KDV Oranı", "Kategori", "Marka" };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        if (rows is not null)
        {
            for (var r = 0; r < rows.Count; r++)
            {
                for (var c = 0; c < rows[r].Length; c++)
                    ws.Cell(r + 2, c + 1).Value = XLCellValue.FromObject(rows[r][c]);
            }
        }

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    private static Stream CreatePriceExcel(List<object[]>? rows = null)
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");

        var headers = new[] { "Barkod", "Liste Fiyatı", "Satış Fiyatı", "Maliyet Fiyatı" };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        if (rows is not null)
        {
            for (var r = 0; r < rows.Count; r++)
            {
                for (var c = 0; c < rows[r].Length; c++)
                    ws.Cell(r + 2, c + 1).Value = XLCellValue.FromObject(rows[r][c]);
            }
        }

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    private static Stream CreateStockExcel(List<object[]>? rows = null)
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");

        var headers = new[] { "Barkod", "Şube ID", "Stok Miktarı" };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        if (rows is not null)
        {
            for (var r = 0; r < rows.Count; r++)
            {
                for (var c = 0; c < rows[r].Length; c++)
                    ws.Cell(r + 2, c + 1).Value = XLCellValue.FromObject(rows[r][c]);
            }
        }

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public async Task ValidateImportAsync_ProductImport_ValidFile_ReturnsPreview()
    {
        // Arrange
        var rows = new List<object[]>
        {
            new object[] { "8680000000001", "Test Ürün 1", "TST-001", 100m, 90m, 50m, 20m, "Giyim", "Nike" },
            new object[] { "8680000000002", "Test Ürün 2", "TST-002", 200m, 180m, 100m, 18m, "Ayakkabı", "Adidas" }
        };
        using var stream = CreateProductExcel(rows);

        // Act
        var result = await _manager.ValidateImportAsync(stream, BulkOperationType.ProductImport);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.TotalRows.Should().Be(2);
        result.Data.ValidRows.Should().Be(2);
        result.Data.InvalidRows.Should().Be(0);
        result.Data.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateImportAsync_ProductImport_WithErrors_ReturnsErrorsInPreview()
    {
        // Arrange — one valid, one invalid (empty barcode)
        var rows = new List<object[]>
        {
            new object[] { "8680000000001", "Test Ürün 1", "TST-001", 100m, 90m, 50m, 20m, "Giyim", "Nike" },
            new object[] { "", "No Barcode", "TST-002", 200m, 180m, 100m, 18m, "Ayakkabı", "Adidas" }
        };
        using var stream = CreateProductExcel(rows);

        // Act
        var result = await _manager.ValidateImportAsync(stream, BulkOperationType.ProductImport);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.TotalRows.Should().Be(2);
        result.Data.ValidRows.Should().Be(1);
        result.Data.InvalidRows.Should().Be(1);
        result.Data.Errors.Should().HaveCount(1);
        result.Data.Errors[0].ErrorMessage.Should().Contain("Barkod boş");
    }

    [Fact]
    public async Task ValidateImportAsync_PriceImport_ValidFile_ReturnsPreview()
    {
        // Arrange
        var rows = new List<object[]>
        {
            new object[] { "8680000000001", 120m, 100m, 60m }
        };
        using var stream = CreatePriceExcel(rows);

        // Act
        var result = await _manager.ValidateImportAsync(stream, BulkOperationType.PriceImport);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.TotalRows.Should().Be(1);
        result.Data.ValidRows.Should().Be(1);
        result.Data.InvalidRows.Should().Be(0);
    }

    [Fact]
    public async Task ValidateImportAsync_StockImport_ValidFile_ReturnsPreview()
    {
        // Arrange
        var rows = new List<object[]>
        {
            new object[] { "8680000000001", 1, 50 }
        };
        using var stream = CreateStockExcel(rows);

        // Act
        var result = await _manager.ValidateImportAsync(stream, BulkOperationType.StockImport);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.TotalRows.Should().Be(1);
        result.Data.ValidRows.Should().Be(1);
        result.Data.InvalidRows.Should().Be(0);
    }

    [Fact]
    public async Task ValidateImportAsync_EmptyFile_ReturnsError()
    {
        // Arrange
        using var stream = CreateProductExcel();

        // Act
        var result = await _manager.ValidateImportAsync(stream, BulkOperationType.ProductImport);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("boş");
    }

    [Fact]
    public async Task ValidateImportAsync_DuplicateBarcodes_DetectedInPreview()
    {
        // Arrange — two rows with same barcode
        var rows = new List<object[]>
        {
            new object[] { "8680000000001", "Test Ürün 1", "TST-001", 100m, 90m, 50m, 20m, "Giyim", "Nike" },
            new object[] { "8680000000001", "Test Ürün 1 Dup", "TST-002", 110m, 95m, 55m, 20m, "Giyim", "Nike" }
        };
        using var stream = CreateProductExcel(rows);

        // Act
        var result = await _manager.ValidateImportAsync(stream, BulkOperationType.ProductImport);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Errors.Should().Contain(e => e.ErrorMessage.Contains("tekrarlayan barkod"));
    }
}
