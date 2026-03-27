using System.Text;
using Entegrasyon.Business.Concrete.BulkOperations;

namespace Entegrasyon.UnitTest.BulkOperations;

public class CsvParserTests
{
    private readonly CsvParser _parser = new();

    [Fact]
    public void ParseProductImport_ValidCsv_ReturnsParsedRows()
    {
        // Arrange
        var csv = "Barkod;Ürün Adı;Stok Kodu;Liste Fiyatı;Satış Fiyatı;Maliyet Fiyatı;KDV Oranı;Kategori;Marka\n"
                + "8680000000001;Test Ürün;TST-001;100;90;50;20;Giyim;Nike\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray());

        // Act
        var result = _parser.ParseProductImport(stream);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data[0].Barcode.Should().Be("8680000000001");
        result.Data[0].Title.Should().Be("Test Ürün");
        result.Data[0].ListPrice.Should().Be(100);
        result.Data[0].SalePrice.Should().Be(90);
    }

    [Fact]
    public void ParseProductImport_EmptyCsv_ReturnsEmptyList()
    {
        // Arrange — headers only
        var csv = "Barkod;Ürün Adı;Stok Kodu;Liste Fiyatı;Satış Fiyatı;Maliyet Fiyatı;KDV Oranı;Kategori;Marka\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = _parser.ParseProductImport(stream);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public void ParseProductImport_WrongHeaders_ReturnsError()
    {
        // Arrange
        var csv = "WrongCol;Col2\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = _parser.ParseProductImport(stream);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("sütun");
    }

    [Fact]
    public void ParsePriceImport_ValidCsv_ReturnsParsedRows()
    {
        // Arrange
        var csv = "Barkod;Liste Fiyatı;Satış Fiyatı;Maliyet Fiyatı\n"
                + "8680000000001;120;110;60\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = _parser.ParsePriceImport(stream);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data[0].Barcode.Should().Be("8680000000001");
        result.Data[0].ListPrice.Should().Be(120);
    }

    [Fact]
    public void ParseStockImport_ValidCsv_ReturnsParsedRows()
    {
        // Arrange
        var csv = "Barkod;Şube ID;Stok Miktarı\n"
                + "8680000000001;1;50\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = _parser.ParseStockImport(stream);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data[0].Barcode.Should().Be("8680000000001");
        result.Data[0].BranchOfficeId.Should().Be(1);
        result.Data[0].Quantity.Should().Be(50);
    }

    [Fact]
    public void ParseProductImport_TurkishCharsWithBom_ParsesCorrectly()
    {
        // Arrange — UTF-8 BOM + Turkish characters
        var csv = "Barkod;Ürün Adı;Stok Kodu;Liste Fiyatı;Satış Fiyatı;Maliyet Fiyatı;KDV Oranı;Kategori;Marka\n"
                + "8680000000001;Çiçek Şapka Öğütücü;ÇŞÖ-001;100;90;50;20;Giyim;Güneş\n";
        var bom = Encoding.UTF8.GetPreamble();
        var bytes = bom.Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        using var stream = new MemoryStream(bytes);

        // Act
        var result = _parser.ParseProductImport(stream);

        // Assert
        result.Success.Should().BeTrue();
        result.Data[0].Title.Should().Be("Çiçek Şapka Öğütücü");
        result.Data[0].BrandName.Should().Be("Güneş");
    }
}
