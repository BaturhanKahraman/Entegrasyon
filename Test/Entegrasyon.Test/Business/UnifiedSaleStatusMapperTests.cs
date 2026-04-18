using Entegrasyon.Business.Mappers;
using Entegrasyon.Entity.Sales;
using FluentAssertions;
using Xunit;

namespace Entegrasyon.UnitTest.Business;

public class UnifiedSaleStatusMapperTests
{
    [Theory]
    [InlineData(1, 0, UnifiedSaleStatus.Completed)]
    [InlineData(2, 0, UnifiedSaleStatus.PartialReturn)]
    [InlineData(3, 0, UnifiedSaleStatus.FullReturn)]
    [InlineData(4, 0, UnifiedSaleStatus.Cancelled)]
    public void Map_ReturnsNormalizedStatus_ForSale(int rawCode, int entityType, UnifiedSaleStatus expected)
    {
        var result = UnifiedSaleStatusMapper.Map(rawCode, entityType);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 1, UnifiedSaleStatus.Pending)]
    [InlineData(1, 1, UnifiedSaleStatus.Pending)]
    [InlineData(2, 1, UnifiedSaleStatus.Pending)]
    [InlineData(3, 1, UnifiedSaleStatus.Shipping)]
    [InlineData(4, 1, UnifiedSaleStatus.Completed)]
    [InlineData(5, 1, UnifiedSaleStatus.Cancelled)]
    public void Map_ReturnsNormalizedStatus_ForOrder(int rawCode, int entityType, UnifiedSaleStatus expected)
    {
        var result = UnifiedSaleStatusMapper.Map(rawCode, entityType);
        result.Should().Be(expected);
    }
}
