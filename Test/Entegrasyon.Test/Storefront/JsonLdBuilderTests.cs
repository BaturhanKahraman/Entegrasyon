using System.Text.Json;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Storefront;
using Entegrasyon.Storefront.Infrastructure;
using FluentAssertions;

namespace Entegrasyon.Test.Storefront;

public class JsonLdBuilderTests
{
    [Fact]
    public void BuildStore_ValidSettings_ReturnsValidJsonLd()
    {
        // Arrange
        var settings = new StorefrontSettings
        {
            StoreName = "Test Mağaza",
            ContactPhone = "+905551234567",
            ContactEmail = "info@testmagaza.com",
            Address = "Atatürk Cad. No:1",
            City = "İstanbul",
            District = "Kadıköy",
            CompanyName = "Test A.Ş.",
            CompanyTaxOffice = "Kadıköy",
            CompanyTaxNumber = "1234567890",
            InstagramUrl = "https://instagram.com/testmagaza",
            FacebookUrl = "https://facebook.com/testmagaza",
            TwitterUrl = null
        };

        // Act
        var json = JsonLdBuilder.BuildStore(settings);

        // Assert
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("@context").GetString().Should().Be("https://schema.org");
        root.GetProperty("@type").GetString().Should().Be("Store");
        root.GetProperty("name").GetString().Should().Be("Test Mağaza");
        root.GetProperty("telephone").GetString().Should().Be("+905551234567");
        root.GetProperty("email").GetString().Should().Be("info@testmagaza.com");

        var address = root.GetProperty("address");
        address.GetProperty("@type").GetString().Should().Be("PostalAddress");
        address.GetProperty("streetAddress").GetString().Should().Be("Atatürk Cad. No:1");
        address.GetProperty("addressLocality").GetString().Should().Be("Kadıköy");
        address.GetProperty("addressRegion").GetString().Should().Be("İstanbul");
        address.GetProperty("addressCountry").GetString().Should().Be("TR");

        var sameAs = root.GetProperty("sameAs");
        sameAs.GetArrayLength().Should().Be(2);
        sameAs[0].GetString().Should().Be("https://instagram.com/testmagaza");
        sameAs[1].GetString().Should().Be("https://facebook.com/testmagaza");
    }

    [Fact]
    public void BuildStore_NoSocialLinks_OmitsSameAs()
    {
        // Arrange
        var settings = new StorefrontSettings
        {
            StoreName = "Minimal Mağaza",
            ContactPhone = "+905550000000",
            ContactEmail = "info@minimal.com",
            Address = "Test Sokak",
            City = "Ankara",
            CompanyName = "Minimal A.Ş.",
            CompanyTaxOffice = "Çankaya",
            CompanyTaxNumber = "9876543210"
        };

        // Act
        var json = JsonLdBuilder.BuildStore(settings);

        // Assert
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("sameAs", out _).Should().BeFalse();
    }

    [Fact]
    public void BuildBreadcrumb_MultipleItems_ReturnsValidBreadcrumbList()
    {
        // Arrange
        var items = new List<(string Name, string Url)>
        {
            ("Ana Sayfa", "https://example.com/"),
            ("Elektronik", "https://example.com/elektronik"),
            ("Telefonlar", "https://example.com/elektronik/telefonlar")
        };

        // Act
        var json = JsonLdBuilder.BuildBreadcrumb(items);

        // Assert
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("@context").GetString().Should().Be("https://schema.org");
        root.GetProperty("@type").GetString().Should().Be("BreadcrumbList");

        var elements = root.GetProperty("itemListElement");
        elements.GetArrayLength().Should().Be(3);

        var first = elements[0];
        first.GetProperty("@type").GetString().Should().Be("ListItem");
        first.GetProperty("position").GetInt32().Should().Be(1);
        first.GetProperty("item").GetProperty("@id").GetString().Should().Be("https://example.com/");
        first.GetProperty("item").GetProperty("name").GetString().Should().Be("Ana Sayfa");

        var third = elements[2];
        third.GetProperty("position").GetInt32().Should().Be(3);
        third.GetProperty("item").GetProperty("name").GetString().Should().Be("Telefonlar");
    }

    [Fact]
    public void BuildProduct_ValidProduct_ReturnsProductSchema()
    {
        var detail = new StorefrontProductDetailDto(
            Guid.NewGuid(), "Siyah Tisort", "Pamuk", "TSR-001",
            "siyah-tisort", null, null, "TestBrand", "testbrand",
            "Tisort", "tisort", 1, 199.90m, 299.90m,
            new List<StorefrontVariantDto>
            {
                new(Guid.NewGuid(), "TSR-001-S", 299.90m, 199.90m, 5,
                    new List<StorefrontVariantAttributeDto> { new("Beden", "S") },
                    new List<string> { "/img/1.jpg" })
            },
            new List<StorefrontAttributeDto>(),
            new List<BreadcrumbItemDto>());

        var json = JsonLdBuilder.BuildProduct(detail, "https://test.com");
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("@type").GetString().Should().Be("Product");
        root.GetProperty("name").GetString().Should().Be("Siyah Tisort");
        root.GetProperty("brand").GetProperty("name").GetString().Should().Be("TestBrand");
        root.GetProperty("offers").GetProperty("lowPrice").GetDecimal().Should().Be(199.90m);
    }

    [Fact]
    public void BuildProduct_OutOfStock_SetsOutOfStockAvailability()
    {
        var detail = new StorefrontProductDetailDto(
            Guid.NewGuid(), "Test Product", null, null,
            "test-product", null, null, null, null,
            "Category", "category", 1, 100m, 100m,
            new List<StorefrontVariantDto>
            {
                new(Guid.NewGuid(), "B001", 100m, 100m, 0,
                    new List<StorefrontVariantAttributeDto>(),
                    new List<string>())
            },
            new List<StorefrontAttributeDto>(),
            new List<BreadcrumbItemDto>());

        var json = JsonLdBuilder.BuildProduct(detail, "https://test.com");
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("offers")
            .GetProperty("availability").GetString()
            .Should().Be("https://schema.org/OutOfStock");
    }

    [Fact]
    public void BuildCollectionPage_ReturnsCorrectSchema()
    {
        var json = JsonLdBuilder.BuildCollectionPage("Tisort", "Tisortler", "https://test.com/kategori/tisort");
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("@type").GetString().Should().Be("CollectionPage");
        doc.RootElement.GetProperty("name").GetString().Should().Be("Tisort");
    }

    [Fact]
    public void BuildCollectionPage_NullDescription_OmitsDescription()
    {
        var json = JsonLdBuilder.BuildCollectionPage("Marka", null, "https://test.com/marka/test");
        var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("description", out _).Should().BeFalse();
    }
}
