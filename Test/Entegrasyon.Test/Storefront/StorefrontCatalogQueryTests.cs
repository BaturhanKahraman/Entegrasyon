using Entegrasyon.Entity.Dtos.Storefront;
using FluentAssertions;

namespace Entegrasyon.UnitTest.Storefront;

public class StorefrontCatalogQueryTests
{
    [Fact]
    public void DefaultQuery_HasCorrectDefaults()
    {
        var query = new StorefrontCatalogQuery();
        query.Page.Should().Be(1);
        query.PageSize.Should().Be(24);
        query.CategoryId.Should().BeNull();
    }

    [Fact]
    public void Query_WithParameters_SetsCorrectly()
    {
        var query = new StorefrontCatalogQuery(CategoryId: 5, BrandId: 3, SearchQuery: "test", Page: 2, PageSize: 12);
        query.CategoryId.Should().Be(5);
        query.BrandId.Should().Be(3);
        query.SearchQuery.Should().Be("test");
        query.Page.Should().Be(2);
        query.PageSize.Should().Be(12);
    }
}
