using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.POS;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.POS;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.POS;
using Entegrasyon.Entity.Sales;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.UnitTest.POS;

public class POSSessionManagerTests : BaseTest
{
    private readonly POSSessionManager _manager;
    private readonly Mock<ISaleManager> _mockSaleManager = new();
    private readonly Mock<ILogger<POSSessionManager>> _mockLogger = new();

    public POSSessionManagerTests()
    {
        MockValidator = new Mock<IFluentValidator>();
        MockValidator
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<OpenSessionDto>()))
            .Returns(Task.CompletedTask);
        MockValidator
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<CloseSessionDto>()))
            .Returns(Task.CompletedTask);
        MockValidator
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<POSTransactionDto>()))
            .Returns(Task.CompletedTask);
        MockValidator
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<AddCashMovementDto>()))
            .Returns(Task.CompletedTask);

        _manager = new POSSessionManager(
            mockContextFactory.Object,
            _mockSaleManager.Object,
            MockValidator.Object,
            mockApplicationLogger.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task OpenSession_ValidDto_CreatesSessionSuccessfully()
    {
        // Arrange
        var dto = new OpenSessionDto(1, Guid.NewGuid(), 500m, "T1");
        mockIntegrationDbContext.Setup(x => x.POSSessions).ReturnsDbSet(new List<POSSession>());

        // Act
        var result = await _manager.OpenSessionAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Status.Should().Be(POSSessionStatus.Open);
        result.Data.BranchOfficeId.Should().Be(1);
        result.Data.OpeningCash.Should().Be(500m);
        result.Data.TerminalId.Should().Be("T1");
    }

    [Fact]
    public async Task OpenSession_AlreadyOpenSession_ReturnsError()
    {
        // Arrange
        var dto = new OpenSessionDto(1, Guid.NewGuid(), 500m, "T1");
        var existingSession = new POSSession
        {
            Id = 1,
            BranchOfficeId = 1,
            Status = POSSessionStatus.Open,
            TerminalId = "T1"
        };
        mockIntegrationDbContext.Setup(x => x.POSSessions).ReturnsDbSet(new List<POSSession> { existingSession });

        // Act
        var result = await _manager.OpenSessionAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("zaten acik");
    }

    [Fact]
    public async Task CloseSession_ValidDto_ClosesAndCalculatesSummary()
    {
        // Arrange
        var session = new POSSession
        {
            Id = 1,
            BranchOfficeId = 1,
            Status = POSSessionStatus.Open,
            OpeningCash = 500m,
            OpenedAt = DateTimeOffset.UtcNow.AddHours(-8)
        };
        var transactions = new List<POSTransaction>
        {
            new() { Id = 1, POSSessionId = 1, CashReceived = 100m, ChangeGiven = 0m },
            new() { Id = 2, POSSessionId = 1, CashReceived = 0m, ChangeGiven = 0m }
        };
        var cashMovements = new List<CashMovement>();

        mockIntegrationDbContext.Setup(x => x.POSSessions).ReturnsDbSet(new List<POSSession> { session });
        mockIntegrationDbContext.Setup(x => x.POSTransactions).ReturnsDbSet(transactions);
        mockIntegrationDbContext.Setup(x => x.CashMovements).ReturnsDbSet(cashMovements);

        var dto = new CloseSessionDto(1, 600m);

        // Act
        var result = await _manager.CloseSessionAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CloseSession_AlreadyClosed_ReturnsError()
    {
        // Arrange
        var session = new POSSession
        {
            Id = 1,
            BranchOfficeId = 1,
            Status = POSSessionStatus.Closed,
            OpeningCash = 500m
        };
        mockIntegrationDbContext.Setup(x => x.POSSessions).ReturnsDbSet(new List<POSSession> { session });

        var dto = new CloseSessionDto(1, 600m);

        // Act
        var result = await _manager.CloseSessionAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("acik degil");
    }

    [Fact]
    public async Task RecordTransaction_ValidSale_CreatesPOSTransactionAndCallsMakeSale()
    {
        // Arrange
        var session = new POSSession
        {
            Id = 1,
            BranchOfficeId = 1,
            Status = POSSessionStatus.Open,
            OpeningCash = 500m
        };
        mockIntegrationDbContext.Setup(x => x.POSSessions).ReturnsDbSet(new List<POSSession> { session });
        mockIntegrationDbContext.Setup(x => x.POSTransactions).ReturnsDbSet(new List<POSTransaction>());

        var saleDto = new MakeSaleDto(
            Guid.NewGuid(),
            1,
            0,
            1,
            new[] { new SaleItemDto(Guid.NewGuid(), 20, 0, 100m, 1, "") });

        var transactionDto = new POSTransactionDto(1, saleDto, PaymentMethod.Cash, 100m, null);

        _mockSaleManager
            .Setup(x => x.MakeSale(It.IsAny<MakeSaleDto>()))
            .ReturnsAsync(new SuccessResult(Messages.SaleSuccess));

        // Act
        var result = await _manager.RecordTransactionAsync(transactionDto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        _mockSaleManager.Verify(x => x.MakeSale(saleDto), Times.Once);
    }

    [Fact]
    public async Task RecordTransaction_NoOpenSession_ReturnsError()
    {
        // Arrange
        var session = new POSSession
        {
            Id = 1,
            BranchOfficeId = 1,
            Status = POSSessionStatus.Closed,
            OpeningCash = 500m
        };
        mockIntegrationDbContext.Setup(x => x.POSSessions).ReturnsDbSet(new List<POSSession> { session });

        var saleDto = new MakeSaleDto(Guid.NewGuid(), 1, 0, 1,
            new[] { new SaleItemDto(Guid.NewGuid(), 20, 0, 100m, 1, "") });
        var transactionDto = new POSTransactionDto(1, saleDto, PaymentMethod.Cash, 100m, null);

        // Act
        var result = await _manager.RecordTransactionAsync(transactionDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("acik degil");
    }

    [Fact]
    public async Task AddCashMovement_ValidDto_RecordsCashMovement()
    {
        // Arrange
        var session = new POSSession
        {
            Id = 1,
            BranchOfficeId = 1,
            Status = POSSessionStatus.Open,
            OpeningCash = 500m
        };
        mockIntegrationDbContext.Setup(x => x.POSSessions).ReturnsDbSet(new List<POSSession> { session });
        mockIntegrationDbContext.Setup(x => x.CashMovements).ReturnsDbSet(new List<CashMovement>());

        var dto = new AddCashMovementDto(1, CashMovementType.CashIn, 200m, "Bozuk para eklendi");

        // Act
        var result = await _manager.AddCashMovementAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetSessionSummary_ReturnsCorrectTotals()
    {
        // Arrange
        var session = new POSSession
        {
            Id = 1,
            BranchOfficeId = 1,
            Status = POSSessionStatus.Open,
            OpeningCash = 500m,
            OpenedAt = DateTimeOffset.UtcNow.AddHours(-4)
        };

        var sale1 = new Sale { Id = Guid.NewGuid(), BranchOfficeId = 1, SaleItems = new List<SaleItem> { new() { UnitPrice = 150m, Quantity = 1, DiscountPercent = 0 } } };
        var sale2 = new Sale { Id = Guid.NewGuid(), BranchOfficeId = 1, SaleItems = new List<SaleItem> { new() { UnitPrice = 200m, Quantity = 2, DiscountPercent = 0 } } };

        var transactions = new List<POSTransaction>
        {
            new() { Id = 1, POSSessionId = 1, SaleId = sale1.Id, CashReceived = 150m, ChangeGiven = 0m, Sale = sale1 },
            new() { Id = 2, POSSessionId = 1, SaleId = sale2.Id, CashReceived = 0m, ChangeGiven = 0m, Sale = sale2 }
        };
        var cashMovements = new List<CashMovement>
        {
            new() { Id = 1, POSSessionId = 1, MovementType = CashMovementType.CashIn, Amount = 100m }
        };

        mockIntegrationDbContext.Setup(x => x.POSSessions).ReturnsDbSet(new List<POSSession> { session });
        mockIntegrationDbContext.Setup(x => x.POSTransactions).ReturnsDbSet(transactions);
        mockIntegrationDbContext.Setup(x => x.CashMovements).ReturnsDbSet(cashMovements);

        // Act
        var result = await _manager.GetSessionSummaryAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.SessionId.Should().Be(1);
        result.Data.OpeningCash.Should().Be(500m);
        result.Data.TransactionCount.Should().Be(2);
        result.Data.TotalCash.Should().Be(150m);
        result.Data.TotalCard.Should().Be(400m); // 200 * 2
    }
}
