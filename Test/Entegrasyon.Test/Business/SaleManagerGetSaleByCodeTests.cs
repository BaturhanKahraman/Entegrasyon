using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Sales;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Business;

public class SaleManagerGetSaleByCodeTests : BaseTest
{
    private readonly SaleManager _sut;

    public SaleManagerGetSaleByCodeTests()
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

    private Sale BuildSale(string saleNumber, string? returnCode)
    {
        return new Sale
        {
            Id = Guid.NewGuid(),
            SaleNumber = saleNumber,
            ReturnCode = returnCode,
            SaleDate = DateTimeOffset.UtcNow,
            SaleSource = SaleSource.POS,
            SaleStatus = SaleStatus.Completed,
            BranchOfficeId = 1,
            GeneralDiscount = 0m,
            SaleItems = [],
            Payments = [],
            Returns = [],
            BranchOffice = new BranchOffice { Id = 1, Name = "Merkez" }
        };
    }

    [Fact]
    public async Task GetSaleByCodeAsync_WithReturnCode_FindsSale()
    {
        var sale = BuildSale("S20260419-0001", "R-ABCDEFGHJKMNP");
        mockIntegrationDbContext.Setup(x => x.Sales).ReturnsDbSet([sale]);

        var result = await _sut.GetSaleByCodeAsync("R-ABCDEFGHJKMNP");

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(sale.Id);
    }

    [Fact]
    public async Task GetSaleByCodeAsync_WithSaleNumber_FindsSale()
    {
        var sale = BuildSale("S20260419-0042", "R-XYZ0123456789");
        mockIntegrationDbContext.Setup(x => x.Sales).ReturnsDbSet([sale]);

        var result = await _sut.GetSaleByCodeAsync("S20260419-0042");

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(sale.Id);
    }

    [Fact]
    public async Task GetSaleByCodeAsync_LowercaseInput_MatchesUpperCodeInDb()
    {
        var sale = BuildSale("S20260419-0001", "R-ABCDEFGHJKMNP");
        mockIntegrationDbContext.Setup(x => x.Sales).ReturnsDbSet([sale]);

        var result = await _sut.GetSaleByCodeAsync("r-abcdefghjkmnp");

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(sale.Id);
    }

    [Fact]
    public async Task GetSaleByCodeAsync_WhitespaceInput_Trimmed()
    {
        var sale = BuildSale("S20260419-0001", "R-ABCDEFGHJKMNP");
        mockIntegrationDbContext.Setup(x => x.Sales).ReturnsDbSet([sale]);

        var result = await _sut.GetSaleByCodeAsync("  R-ABCDEFGHJKMNP  ");

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(sale.Id);
    }

    [Fact]
    public async Task GetSaleByCodeAsync_NotFound_ReturnsError()
    {
        mockIntegrationDbContext.Setup(x => x.Sales).ReturnsDbSet(new List<Sale>());

        var result = await _sut.GetSaleByCodeAsync("R-UNKNOWN000000");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadı");
    }

    [Fact]
    public async Task GetSaleByCodeAsync_EmptyInput_ReturnsError()
    {
        var result = await _sut.GetSaleByCodeAsync("");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("boş");
    }

    [Fact]
    public async Task GetSaleByCodeAsync_WhitespaceOnlyInput_ReturnsError()
    {
        var result = await _sut.GetSaleByCodeAsync("   ");

        result.Success.Should().BeFalse();
    }
}
