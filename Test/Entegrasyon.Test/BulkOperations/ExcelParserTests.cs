using ClosedXML.Excel;
using Entegrasyon.Business.Concrete.BulkOperations;

namespace Entegrasyon.UnitTest.BulkOperations;

public class ExcelParserTests
{
    private readonly ExcelParser _parser = new();

    private static Stream CreateProductExcel(bool includeHeaders = true, List<object[]>? rows = null)
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");

        if (includeHeaders)
        {
            var headers = new[] { "Barkod", "Ürün Adı", "Stok Kodu", "Liste Fiyatı", "Satış Fiyatı", "Maliyet Fiyatı", "KDV Oranı", "Kategori", "Marka" };
            for (var i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];
        }

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
    public void ParseProductImport_ValidExcel_ReturnsRows()
    {
        // Arrange
        var rows = new List<object[]>
        {
            new object[] { "8680000000001", "Test Ürün 1", "TST-001", 100m, 90m, 50m, 20m, "Giyim", "Nike" },
            new object[] { "8680000000002", "Test Ürün 2", "TST-002", 200m, 180m, 100m, 18m, "Ayakkabı", "Adidas" }
        };
        using var stream = CreateProductExcel(rows: rows);

        // Act
        var result = _parser.ParseProductImport(stream);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data[0].Barcode.Should().Be("8680000000001");
        result.Data[0].Title.Should().Be("Test Ürün 1");
        result.Data[0].ListPrice.Should().Be(100m);
        result.Data[1].Barcode.Should().Be("8680000000002");
    }

    [Fact]
    public void ParseProductImport_MissingRequiredColumn_ReturnsError()
    {
        // Arrange — wrong headers
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");
        ws.Cell(1, 1).Value = "WrongHeader";
        ws.Cell(1, 2).Value = "AnotherWrong";

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        // Act
        var result = _parser.ParseProductImport(ms);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Eksik veya hatalı sütun başlıkları");
    }

    [Fact]
    public void ParsePriceImport_ValidExcel_ReturnsPriceRows()
    {
        // Arrange
        var rows = new List<object[]>
        {
            new object[] { "8680000000001", 120m, 100m, 60m },
            new object[] { "8680000000002", 250m, 220m, 130m }
        };
        using var stream = CreatePriceExcel(rows);

        // Act
        var result = _parser.ParsePriceImport(stream);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data[0].Barcode.Should().Be("8680000000001");
        result.Data[0].ListPrice.Should().Be(120m);
        result.Data[1].SalePrice.Should().Be(220m);
    }

    [Fact]
    public void ParseStockImport_ValidExcel_ReturnsStockRows()
    {
        // Arrange
        var rows = new List<object[]>
        {
            new object[] { "8680000000001", 1, 50 },
            new object[] { "8680000000002", 2, 30 }
        };
        using var stream = CreateStockExcel(rows);

        // Act
        var result = _parser.ParseStockImport(stream);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data[0].Barcode.Should().Be("8680000000001");
        result.Data[0].BranchOfficeId.Should().Be(1);
        result.Data[0].Quantity.Should().Be(50);
    }
}
