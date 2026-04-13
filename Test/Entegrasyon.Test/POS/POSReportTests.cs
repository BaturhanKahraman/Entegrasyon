using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.POS;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.POS;
using Entegrasyon.Entity.POS;
using Entegrasyon.Entity.Sales;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.UnitTest.POS;

public class POSReportTests : BaseTest
{
    private readonly POSSessionManager _manager;
    private readonly Mock<ISaleManager> _mockSaleManager = new();
    private readonly Mock<ILogger<POSSessionManager>> _mockLogger = new();

    public POSReportTests()
    {
        MockValidator = new Mock<IFluentValidator>();

        _manager = new POSSessionManager(
            mockContextFactory.Object,
            _mockSaleManager.Object,
            MockValidator.Object,
            mockApplicationLogger.Object,
            _mockLogger.Object);
    }

    private static (PaymentMethodDefinition cash, PaymentMethodDefinition card) BuildPaymentMethods() =>
        (
            new PaymentMethodDefinition { Id = 1, Name = "Nakit", SystemCode = "Cash", TenantId = 1 },
            new PaymentMethodDefinition { Id = 2, Name = "Kredi Karti", SystemCode = "CreditCard", TenantId = 1 }
        );

    [Fact]
    public async Task GetXReportAsync_SessionNotFound_ReturnsError()
    {
        // Arrange
        mockIntegrationDbContext.Setup(x => x.POSSessions).ReturnsDbSet(new List<POSSession>());

        // Act
        var result = await _manager.GetXReportAsync(999);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadi");
    }

    [Fact]
    public async Task GetXReportAsync_ReturnsPaymentBreakdownByMethod()
    {
        // Arrange
        var (cashMethod, cardMethod) = BuildPaymentMethods();
        var session = new POSSession
        {
            Id = 1,
            BranchOfficeId = 1,
            Status = POSSessionStatus.Open,
            OpeningCash = 500m,
            OpenedAt = DateTimeOffset.UtcNow.AddHours(-4),
            Cashier = new Entity.User.ApplicationUser { Name = "Ali", Surname = "Veli" },
            CashMovements = new List<CashMovement>()
        };

        var sale1Id = Guid.NewGuid();
        var sale2Id = Guid.NewGuid();

        var sales = new List<Sale>
        {
            new()
            {
                Id = sale1Id,
                BranchOfficeId = 1,
                SaleStatus = SaleStatus.Completed,
                SaleItems = new List<SaleItem> { new() { UnitPrice = 150m, Quantity = 1, TaxPercentage = 18 } },
                Payments = new List<SalePayment>
                {
                    new() { SaleId = sale1Id, PaymentMethodId = 1, PaymentMethod = cashMethod, Amount = 150m }
                }
            },
            new()
            {
                Id = sale2Id,
                BranchOfficeId = 1,
                SaleStatus = SaleStatus.Completed,
                SaleItems = new List<SaleItem> { new() { UnitPrice = 200m, Quantity = 2, TaxPercentage = 18 } },
                Payments = new List<SalePayment>
                {
                    new() { SaleId = sale2Id, PaymentMethodId = 2, PaymentMethod = cardMethod, Amount = 400m }
                }
            }
        };

        var transactions = new List<POSTransaction>
        {
            new() { Id = 1, POSSessionId = 1, SaleId = sale1Id },
            new() { Id = 2, POSSessionId = 1, SaleId = sale2Id }
        };

        mockIntegrationDbContext.Setup(x => x.POSSessions).ReturnsDbSet(new List<POSSession> { session });
        mockIntegrationDbContext.Setup(x => x.POSTransactions).ReturnsDbSet(transactions);
        mockIntegrationDbContext.Setup(x => x.Sales).ReturnsDbSet(sales);
        mockIntegrationDbContext.Setup(x => x.SaleReturns).ReturnsDbSet(new List<SaleReturn>());

        // Act
        var result = await _manager.GetXReportAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.TransactionCount.Should().Be(2);
        result.Data.TotalSales.Should().Be(550m); // 150 + 400
        result.Data.TotalReturns.Should().Be(0m);
        result.Data.NetSales.Should().Be(550m);
        result.Data.PaymentBreakdown.Should().HaveCount(2);
        result.Data.PaymentBreakdown.Should().Contain(p => p.MethodName == "Nakit" && p.Total == 150m);
        result.Data.PaymentBreakdown.Should().Contain(p => p.MethodName == "Kredi Karti" && p.Total == 400m);
        result.Data.ActualCash.Should().BeNull(); // X Report — no actual cash
        result.Data.CashDifference.Should().BeNull();
    }

    [Fact]
    public async Task GetXReportAsync_ReturnsVatBreakdownByRate()
    {
        // Arrange
        var (cashMethod, _) = BuildPaymentMethods();
        var session = new POSSession
        {
            Id = 1,
            BranchOfficeId = 1,
            Status = POSSessionStatus.Open,
            OpeningCash = 0m,
            OpenedAt = DateTimeOffset.UtcNow.AddHours(-2),
            Cashier = new Entity.User.ApplicationUser { Name = "Test", Surname = "User" },
            CashMovements = new List<CashMovement>()
        };

        var saleId = Guid.NewGuid();
        var sales = new List<Sale>
        {
            new()
            {
                Id = saleId,
                BranchOfficeId = 1,
                SaleStatus = SaleStatus.Completed,
                SaleItems = new List<SaleItem>
                {
                    new() { UnitPrice = 100m, Quantity = 1, TaxPercentage = 8 },
                    new() { UnitPrice = 200m, Quantity = 1, TaxPercentage = 18 }
                },
                Payments = new List<SalePayment>
                {
                    new() { SaleId = saleId, PaymentMethodId = 1, PaymentMethod = cashMethod, Amount = 300m }
                }
            }
        };

        var transactions = new List<POSTransaction>
        {
            new() { Id = 1, POSSessionId = 1, SaleId = saleId }
        };

        mockIntegrationDbContext.Setup(x => x.POSSessions).ReturnsDbSet(new List<POSSession> { session });
        mockIntegrationDbContext.Setup(x => x.POSTransactions).ReturnsDbSet(transactions);
        mockIntegrationDbContext.Setup(x => x.Sales).ReturnsDbSet(sales);
        mockIntegrationDbContext.Setup(x => x.SaleReturns).ReturnsDbSet(new List<SaleReturn>());

        // Act
        var result = await _manager.GetXReportAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.VatBreakdown.Should().HaveCount(2);
        var vat8 = result.Data.VatBreakdown.First(v => v.VatRate == 8m);
        vat8.TaxBase.Should().Be(100m);
        vat8.VatAmount.Should().Be(8m);
        var vat18 = result.Data.VatBreakdown.First(v => v.VatRate == 18m);
        vat18.TaxBase.Should().Be(200m);
        vat18.VatAmount.Should().Be(36m);
    }

    [Fact]
    public async Task GetXReportAsync_ExcludesCancelledSales()
    {
        // Arrange
        var (cashMethod, _) = BuildPaymentMethods();
        var session = new POSSession
        {
            Id = 1,
            BranchOfficeId = 1,
            Status = POSSessionStatus.Open,
            OpeningCash = 0m,
            OpenedAt = DateTimeOffset.UtcNow.AddHours(-2),
            Cashier = new Entity.User.ApplicationUser { Name = "Test", Surname = "User" },
            CashMovements = new List<CashMovement>()
        };

        var sale1Id = Guid.NewGuid();
        var sale2Id = Guid.NewGuid();
        var sales = new List<Sale>
        {
            new()
            {
                Id = sale1Id,
                BranchOfficeId = 1,
                SaleStatus = SaleStatus.Completed,
                SaleItems = new List<SaleItem> { new() { UnitPrice = 100m, Quantity = 1, TaxPercentage = 18 } },
                Payments = new List<SalePayment>
                {
                    new() { SaleId = sale1Id, PaymentMethodId = 1, PaymentMethod = cashMethod, Amount = 100m }
                }
            },
            new()
            {
                Id = sale2Id,
                BranchOfficeId = 1,
                SaleStatus = SaleStatus.Cancelled,
                SaleItems = new List<SaleItem> { new() { UnitPrice = 500m, Quantity = 1, TaxPercentage = 18 } },
                Payments = new List<SalePayment>
                {
                    new() { SaleId = sale2Id, PaymentMethodId = 1, PaymentMethod = cashMethod, Amount = 500m }
                }
            }
        };

        var transactions = new List<POSTransaction>
        {
            new() { Id = 1, POSSessionId = 1, SaleId = sale1Id },
            new() { Id = 2, POSSessionId = 1, SaleId = sale2Id }
        };

        mockIntegrationDbContext.Setup(x => x.POSSessions).ReturnsDbSet(new List<POSSession> { session });
        mockIntegrationDbContext.Setup(x => x.POSTransactions).ReturnsDbSet(transactions);
        mockIntegrationDbContext.Setup(x => x.Sales).ReturnsDbSet(sales);
        mockIntegrationDbContext.Setup(x => x.SaleReturns).ReturnsDbSet(new List<SaleReturn>());

        // Act
        var result = await _manager.GetXReportAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.TotalSales.Should().Be(100m); // cancelled sale excluded
        result.Data.TransactionCount.Should().Be(1);
    }

    [Fact]
    public async Task GetXReportAsync_SubtractsApprovedReturns()
    {
        // Arrange
        var (cashMethod, _) = BuildPaymentMethods();
        var session = new POSSession
        {
            Id = 1,
            BranchOfficeId = 1,
            Status = POSSessionStatus.Open,
            OpeningCash = 0m,
            OpenedAt = DateTimeOffset.UtcNow.AddHours(-2),
            Cashier = new Entity.User.ApplicationUser { Name = "Test", Surname = "User" },
            CashMovements = new List<CashMovement>()
        };

        var saleId = Guid.NewGuid();
        var sales = new List<Sale>
        {
            new()
            {
                Id = saleId,
                BranchOfficeId = 1,
                SaleStatus = SaleStatus.Completed,
                SaleItems = new List<SaleItem> { new() { UnitPrice = 300m, Quantity = 1, TaxPercentage = 18 } },
                Payments = new List<SalePayment>
                {
                    new() { SaleId = saleId, PaymentMethodId = 1, PaymentMethod = cashMethod, Amount = 300m }
                }
            }
        };

        var saleReturns = new List<SaleReturn>
        {
            new() { SaleId = saleId, ReturnStatus = ReturnStatus.Approved, RefundAmount = 50m },
            new() { SaleId = saleId, ReturnStatus = ReturnStatus.Pending, RefundAmount = 100m } // not approved — excluded
        };

        var transactions = new List<POSTransaction>
        {
            new() { Id = 1, POSSessionId = 1, SaleId = saleId }
        };

        mockIntegrationDbContext.Setup(x => x.POSSessions).ReturnsDbSet(new List<POSSession> { session });
        mockIntegrationDbContext.Setup(x => x.POSTransactions).ReturnsDbSet(transactions);
        mockIntegrationDbContext.Setup(x => x.Sales).ReturnsDbSet(sales);
        mockIntegrationDbContext.Setup(x => x.SaleReturns).ReturnsDbSet(saleReturns);

        // Act
        var result = await _manager.GetXReportAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.TotalSales.Should().Be(300m);
        result.Data.TotalReturns.Should().Be(50m); // only approved
        result.Data.NetSales.Should().Be(250m);
    }

    [Fact]
    public async Task GetZReportAsync_IncludesActualCashAndDifference()
    {
        // Arrange
        var (cashMethod, _) = BuildPaymentMethods();
        var session = new POSSession
        {
            Id = 1,
            BranchOfficeId = 1,
            Status = POSSessionStatus.Closed,
            OpeningCash = 500m,
            ClosingCash = 680m,
            OpenedAt = DateTimeOffset.UtcNow.AddHours(-8),
            ClosedAt = DateTimeOffset.UtcNow,
            Cashier = new Entity.User.ApplicationUser { Name = "Ali", Surname = "Veli" },
            CashMovements = new List<CashMovement>()
        };

        var saleId = Guid.NewGuid();
        var sales = new List<Sale>
        {
            new()
            {
                Id = saleId,
                BranchOfficeId = 1,
                SaleStatus = SaleStatus.Completed,
                SaleItems = new List<SaleItem> { new() { UnitPrice = 150m, Quantity = 1, TaxPercentage = 18 } },
                Payments = new List<SalePayment>
                {
                    new() { SaleId = saleId, PaymentMethodId = 1, PaymentMethod = cashMethod, Amount = 150m }
                }
            }
        };

        var transactions = new List<POSTransaction>
        {
            new() { Id = 1, POSSessionId = 1, SaleId = saleId }
        };

        mockIntegrationDbContext.Setup(x => x.POSSessions).ReturnsDbSet(new List<POSSession> { session });
        mockIntegrationDbContext.Setup(x => x.POSTransactions).ReturnsDbSet(transactions);
        mockIntegrationDbContext.Setup(x => x.Sales).ReturnsDbSet(sales);
        mockIntegrationDbContext.Setup(x => x.SaleReturns).ReturnsDbSet(new List<SaleReturn>());

        // Act
        var result = await _manager.GetZReportAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.ActualCash.Should().Be(680m);
        // ExpectedCash = OpeningCash(500) + TotalCash(150) + CashMovementsNet(0) = 650
        result.Data.ExpectedCash.Should().Be(650m);
        result.Data.CashDifference.Should().Be(30m); // 680 - 650
    }
}
