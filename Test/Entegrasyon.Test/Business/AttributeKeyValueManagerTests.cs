using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Products;

namespace Entegrasyon.UnitTest.Business;

public class AttributeKeyValueManagerTests : BaseTest
{
    private readonly IAttributeKeyValueManager _manager;

    public AttributeKeyValueManagerTests()
    {
        _manager = new AttributeKeyValueManager(mockContextFactory.Object);
    }

    [Fact]
    public void ClearEmptyAttributes_ShouldRemove_WhenValueIdNullAndNoCustomValue()
    {
        // Arrange
        var product = new Product
        {
            AttributeKeyValues = new List<AttributeKeyValue>
            {
                new() { AttributeValueId = null, CustomValue = null }
            }
        };

        // Act
        _manager.ClearEmptyAttributes(product);

        // Assert
        product.AttributeKeyValues.Should().BeEmpty();
    }

    [Fact]
    public void ClearEmptyAttributes_ShouldRemove_WhenValueIdZeroAndNoCustomValue()
    {
        // Arrange
        var product = new Product
        {
            AttributeKeyValues = new List<AttributeKeyValue>
            {
                new() { AttributeValueId = 0, CustomValue = string.Empty }
            }
        };

        // Act
        _manager.ClearEmptyAttributes(product);

        // Assert
        product.AttributeKeyValues.Should().BeEmpty();
    }

    [Fact]
    public void ClearEmptyAttributes_ShouldKeep_WhenValueIdIsPositive()
    {
        // Arrange
        var product = new Product
        {
            AttributeKeyValues = new List<AttributeKeyValue>
            {
                new() { AttributeValueId = 5, CustomValue = null }
            }
        };

        // Act
        _manager.ClearEmptyAttributes(product);

        // Assert
        product.AttributeKeyValues.Should().HaveCount(1);
    }

    [Fact]
    public void ClearEmptyAttributes_ShouldKeep_WhenValueIdZeroButCustomValueProvided()
    {
        // Arrange
        var product = new Product
        {
            AttributeKeyValues = new List<AttributeKeyValue>
            {
                new() { AttributeValueId = 0, CustomValue = "Kırmızı" }
            }
        };

        // Act
        _manager.ClearEmptyAttributes(product);

        // Assert
        product.AttributeKeyValues.Should().HaveCount(1);
        product.AttributeKeyValues.First().CustomValue.Should().Be("Kırmızı");
    }

    [Fact]
    public void ClearEmptyAttributes_ShouldKeep_OnlyValidAttributes_FromMixedList()
    {
        // Arrange — 2 geçerli, 3 geçersiz
        var product = new Product
        {
            AttributeKeyValues = new List<AttributeKeyValue>
            {
                new() { AttributeValueId = 3, CustomValue = null },      // geçerli: id > 0
                new() { AttributeValueId = 0, CustomValue = "Sarı" },   // geçerli: id=0 ama custom var
                new() { AttributeValueId = null, CustomValue = null },   // geçersiz: null id, no custom
                new() { AttributeValueId = 0, CustomValue = "" },        // geçersiz: id=0, no custom
                new() { AttributeValueId = 0, CustomValue = null },      // geçersiz: id=0, null custom
            }
        };

        // Act
        _manager.ClearEmptyAttributes(product);

        // Assert
        product.AttributeKeyValues.Should().HaveCount(2);
    }
}
