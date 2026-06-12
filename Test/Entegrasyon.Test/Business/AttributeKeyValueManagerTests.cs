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
        _manager = new AttributeKeyValueManager();
    }

    [Fact]
    public void ClearEmptyAttributes_ShouldRemove_WhenValueIdIsZero()
    {
        // Arrange
        var product = new Product
        {
            AttributeKeyValues = new List<AttributeKeyValue>
            {
                new() { AttributeValueId = 0 }
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
                new() { AttributeValueId = 5 }
            }
        };

        // Act
        _manager.ClearEmptyAttributes(product);

        // Assert
        product.AttributeKeyValues.Should().HaveCount(1);
    }

    [Fact]
    public void ClearEmptyAttributes_ShouldKeep_OnlyValidAttributes_FromMixedList()
    {
        // Arrange — 2 geçerli, 3 geçersiz
        var product = new Product
        {
            AttributeKeyValues = new List<AttributeKeyValue>
            {
                new() { AttributeValueId = 3 },   // geçerli: id > 0
                new() { AttributeValueId = 7 },   // geçerli: id > 0
                new() { AttributeValueId = 0 },   // geçersiz: id=0
                new() { AttributeValueId = 0 },   // geçersiz: id=0
                new() { AttributeValueId = 0 },   // geçersiz: id=0
            }
        };

        // Act
        _manager.ClearEmptyAttributes(product);

        // Assert
        product.AttributeKeyValues.Should().HaveCount(2);
    }
}
