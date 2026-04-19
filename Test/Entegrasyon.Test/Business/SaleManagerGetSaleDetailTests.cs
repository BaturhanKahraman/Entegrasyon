using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Sales;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;

namespace Entegrasyon.UnitTest.Business;

public class SaleManagerGetSaleDetailTests : BaseTest
{
    private readonly SaleManager _sut;

    public SaleManagerGetSaleDetailTests()
    {
        MockValidator = new Mock<IFluentValidator>();

        var mockOfficeStockManager = new Mock<IOfficeStockManager>();
        var mockLogger = new Mock<ILogger<SaleManager>>();
        var mapper = new SaleMapper();

        _sut = new SaleManager(
            mockContextFactory.Object,
            mockApplicationLogger.Object,
            mockLogger.Object,
            mapper,
            MockValidator.Object,
            mockOfficeStockManager.Object);
    }

    private void SetupDbContextForGetSaleDetail(List<Sale> sales)
    {
        mockIntegrationDbContext
            .Setup(x => x.Sales)
            .ReturnsDbSet(sales);
    }

    [Fact]
    public async Task GetSaleDetailAsync_SubtractsDiscountAmount_FromSubtotalAndGrandTotal()
    {
        // Arrange: bir Sale iki SaleItem ile — biri indirimli
        var saleId = Guid.NewGuid();
        var sale = new Sale
        {
            Id = saleId,
            SaleNumber = "S20260419-0001",
            SaleDate = DateTimeOffset.UtcNow,
            SaleSource = SaleSource.POS,
            SaleStatus = SaleStatus.Completed,
            BranchOfficeId = 1,
            GeneralDiscount = 0m,
            SaleItems =
            [
                new SaleItem
                {
                    Id = Guid.NewGuid(),
                    ProductVariantId = Guid.NewGuid(),
                    UnitPrice = 1000m,
                    Quantity = 1,
                    TaxPercentage = 20,
                    DiscountAmount = 100m,
                    Barcode = "1",
                    ProductTitle = "A"
                },
                new SaleItem
                {
                    Id = Guid.NewGuid(),
                    ProductVariantId = Guid.NewGuid(),
                    UnitPrice = 500m,
                    Quantity = 1,
                    TaxPercentage = 20,
                    DiscountAmount = null,
                    Barcode = "2",
                    ProductTitle = "B"
                }
            ],
            Payments = [],
            Returns = [],
            BranchOffice = new BranchOffice { Id = 1, Name = "Merkez" }
        };

        SetupDbContextForGetSaleDetail([sale]);

        // Act
        var result = await _sut.GetSaleDetailAsync(saleId);

        // Assert
        result.Success.Should().BeTrue();
        // SubTotal = (1000 - 100) + 500 = 1400 (KDV hariç, indirim sonrası)
        result.Data.SubTotal.Should().Be(1400m);
        // GrandTotal = 1400 * 1.20 = 1680 (KDV dahil, indirim sonrası)
        result.Data.GrandTotal.Should().Be(1680m);
        result.Data.VatTotal.Should().Be(280m);
    }
}
