using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Products;
using FluentAssertions;
using Xunit;

namespace Entegrasyon.UnitTest.Business;

public class VariantNamingServiceTests
{
    private readonly IVariantNamingService _sut = new VariantNamingService();

    private static Product MakeProduct(string title = "Basic Tshirt") => new() { Title = title };

    private static ProductVariant MakeVariant(params ProductVariantAttribute[] attrs)
        => new() { ProductVariantAttributes = attrs.ToList() };

    [Fact]
    public void Compute_ReturnsVarianterThenSlicer()
    {
        var variant = MakeVariant(
            new ProductVariantAttribute { CategoryAttributeValue = "XL", IsSlicer = true },
            new ProductVariantAttribute { CategoryAttributeValue = "Sarı", IsVarianter = true }
        );

        var result = _sut.Compute(variant, MakeProduct());

        result.Should().Be("Sarı XL");
    }

    [Fact]
    public void Compute_MultipleVarianters_KeepsInsertionOrder()
    {
        var variant = MakeVariant(
            new ProductVariantAttribute { CategoryAttributeValue = "Sarı", IsVarianter = true },
            new ProductVariantAttribute { CategoryAttributeValue = "Çiçekli", IsVarianter = true }
        );

        var result = _sut.Compute(variant, MakeProduct());

        result.Should().Be("Sarı Çiçekli");
    }

    [Fact]
    public void Compute_OnlySlicer_ReturnsSlicer()
    {
        var variant = MakeVariant(
            new ProductVariantAttribute { CategoryAttributeValue = "XL", IsSlicer = true }
        );

        _sut.Compute(variant, MakeProduct()).Should().Be("XL");
    }

    [Fact]
    public void Compute_OnlyVarianter_ReturnsVarianter()
    {
        var variant = MakeVariant(
            new ProductVariantAttribute { CategoryAttributeValue = "Sarı", IsVarianter = true }
        );

        _sut.Compute(variant, MakeProduct()).Should().Be("Sarı");
    }

    [Fact]
    public void Compute_NoVarianterNoSlicer_ReturnsProductTitle()
    {
        var variant = MakeVariant(
            new ProductVariantAttribute { CategoryAttributeValue = "Yuvarlak", IsVarianter = false, IsSlicer = false }
        );

        _sut.Compute(variant, MakeProduct("T-shirt")).Should().Be("T-shirt");
    }

    [Fact]
    public void Compute_EmptyAttributes_ReturnsProductTitle()
    {
        var variant = MakeVariant();

        _sut.Compute(variant, MakeProduct("Sade Ürün")).Should().Be("Sade Ürün");
    }

    [Fact]
    public void Compute_UsesCustomValueWhenCategoryValueNull()
    {
        var variant = MakeVariant(
            new ProductVariantAttribute { CategoryAttributeValue = null, CustomValue = "Lacivert", IsVarianter = true }
        );

        _sut.Compute(variant, MakeProduct()).Should().Be("Lacivert");
    }

    [Fact]
    public void Compute_SkipsEmptyValues()
    {
        var variant = MakeVariant(
            new ProductVariantAttribute { CategoryAttributeValue = "", IsVarianter = true },
            new ProductVariantAttribute { CategoryAttributeValue = "XL", IsSlicer = true }
        );

        _sut.Compute(variant, MakeProduct()).Should().Be("XL");
    }

    [Fact]
    public void Compute_MixedOrder_PutsVarianterFirst()
    {
        var variant = MakeVariant(
            new ProductVariantAttribute { CategoryAttributeValue = "XL", IsSlicer = true },
            new ProductVariantAttribute { CategoryAttributeValue = "M", IsSlicer = true },
            new ProductVariantAttribute { CategoryAttributeValue = "Sarı", IsVarianter = true }
        );

        _sut.Compute(variant, MakeProduct()).Should().Be("Sarı XL M");
    }

    [Fact]
    public void Compute_EmptyProductTitleAndNoAttributes_ReturnsNull()
    {
        var result = _sut.Compute(MakeVariant(), new Product { Title = "" });
        result.Should().BeNull();
    }
}
