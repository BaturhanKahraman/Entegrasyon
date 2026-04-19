using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Extensions;
using FluentAssertions;
using Xunit;

namespace Entegrasyon.UnitTest.Business;

public class VariantNameExtensionsTests
{
    [Fact]
    public void Resolve_PrefersStoredName()
    {
        var result = VariantNameExtensions.ResolveDisplayName(
            "Override",
            new[] { new VariantAttributeLite("Sarı", null, true, false, 0) },
            "Tshirt");
        result.Should().Be("Override");
    }

    [Fact]
    public void Resolve_FallsBackToComputeWhenNameNull()
    {
        var result = VariantNameExtensions.ResolveDisplayName(
            null,
            new[]
            {
                new VariantAttributeLite("Sarı", null, true, false, 0),
                new VariantAttributeLite("XL", null, false, true, 1)
            },
            "Tshirt");
        result.Should().Be("Sarı XL");
    }

    [Fact]
    public void Resolve_FallsBackToProductTitleWhenAllEmpty()
    {
        var result = VariantNameExtensions.ResolveDisplayName(null, Array.Empty<VariantAttributeLite>(), "Tshirt");
        result.Should().Be("Tshirt");
    }

    [Fact]
    public void Resolve_TreatsWhitespaceNameAsNull()
    {
        var result = VariantNameExtensions.ResolveDisplayName(
            "   ",
            new[] { new VariantAttributeLite("Sarı", null, true, false, 0) },
            "Tshirt");
        result.Should().Be("Sarı");
    }

    [Fact]
    public void Resolve_NullAttributes_FallsBackToTitle()
    {
        var result = VariantNameExtensions.ResolveDisplayName(null, null, "Tshirt");
        result.Should().Be("Tshirt");
    }

    [Fact]
    public void Resolve_OrdersVarianterBeforeSlicer_InsertionOrderWithinGroup()
    {
        var result = VariantNameExtensions.ResolveDisplayName(
            null,
            new[]
            {
                new VariantAttributeLite("M", null, false, true, 0),
                new VariantAttributeLite("Sarı", null, true, false, 1),
                new VariantAttributeLite("Çiçekli", null, true, false, 2),
                new VariantAttributeLite("XL", null, false, true, 3)
            },
            "Tshirt");
        result.Should().Be("Sarı Çiçekli M XL");
    }
}
