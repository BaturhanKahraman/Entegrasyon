using System.Text;
using Entegrasyon.Business.Concrete.BulkOperations;

namespace Entegrasyon.UnitTest.BulkOperations;

public class CsvWriterTests
{
    [Fact]
    public void WriteCsv_WithHeaders_ProducesValidOutput()
    {
        // Arrange
        var headers = new[] { "Barkod", "Liste Fiyatı", "Satış Fiyatı" };
        var data = new List<string[]>
        {
            new[] { "8680000000001", "100", "90" },
            new[] { "8680000000002", "200", "180" }
        };

        // Act
        var bytes = CsvWriter.WriteCsv(headers, data);

        // Assert
        bytes.Should().NotBeEmpty();
        // Should start with UTF-8 BOM
        bytes[0].Should().Be(0xEF);
        bytes[1].Should().Be(0xBB);
        bytes[2].Should().Be(0xBF);

        var content = Encoding.UTF8.GetString(bytes);
        content.Should().Contain("Barkod;Liste Fiyatı;Satış Fiyatı");
        content.Should().Contain("8680000000001;100;90");
        content.Should().Contain("8680000000002;200;180");
    }

    [Fact]
    public void WriteCsv_EmptyData_ProducesHeadersOnly()
    {
        // Arrange
        var headers = new[] { "Barkod", "Fiyat" };
        var data = new List<string[]>();

        // Act
        var bytes = CsvWriter.WriteCsv(headers, data);

        // Assert
        var content = Encoding.UTF8.GetString(bytes);
        content.Should().Contain("Barkod;Fiyat");
        // Only header line + newline
        content.Trim().Split('\n').Should().HaveCount(1);
    }

    [Fact]
    public void WriteCsv_TurkishChars_PreservedInOutput()
    {
        // Arrange
        var headers = new[] { "Ürün Adı" };
        var data = new List<string[]>
        {
            new[] { "Çiçek Şapka Öğütücü" }
        };

        // Act
        var bytes = CsvWriter.WriteCsv(headers, data);

        // Assert
        var content = Encoding.UTF8.GetString(bytes);
        content.Should().Contain("Çiçek Şapka Öğütücü");
    }

    [Fact]
    public void WriteCsv_ValueContainsSemicolon_IsQuoted()
    {
        // Arrange
        var headers = new[] { "Name" };
        var data = new List<string[]>
        {
            new[] { "Value;With;Semicolons" }
        };

        // Act
        var bytes = CsvWriter.WriteCsv(headers, data);

        // Assert
        var content = Encoding.UTF8.GetString(bytes);
        content.Should().Contain("\"Value;With;Semicolons\"");
    }
}
