using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Invoicing;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Invoicing;
using Entegrasyon.Entity.Invoicing;
using Entegrasyon.Entity.Sales;
using Entegrasyon.Entity.Customers;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.UnitTest.Invoicing;

public class EInvoiceManagerTests : BaseTest
{
    private readonly IEInvoiceManager _manager;
    private readonly Mock<IEInvoiceIntegratorClient> _mockClient = new();

    public EInvoiceManagerTests()
    {
        MockValidator = new Mock<IFluentValidator>();
        MockValidator
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<CreateEInvoiceDto>()))
            .Returns(Task.CompletedTask);

        mockIntegrationDbContext
            .Setup(x => x.EInvoices)
            .ReturnsDbSet(new List<EInvoice>());
        mockIntegrationDbContext
            .Setup(x => x.Sales)
            .ReturnsDbSet(new List<Sale>());
        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _manager = new EInvoiceManager(
            mockContextFactory.Object,
            mockApplicationLogger.Object,
            MockValidator.Object,
            _mockClient.Object);
    }

    private static CreateEInvoiceDto BuildValidDto() => new()
    {
        InvoiceType = EInvoiceType.EFatura,
        CustomerTaxId = "1234567890",
        CustomerTitle = "Test Firma A.S.",
        IssueDate = DateTimeOffset.UtcNow,
        IntegratorProvider = IntegratorProvider.Custom,
        Lines =
        [
            new CreateEInvoiceLineDto("Urun A", 2, 100m, 20),
            new CreateEInvoiceLineDto("Urun B", 1, 50m, 10)
        ]
    };

    [Fact]
    public async Task CreateInvoice_ValidDto_ReturnsSuccessWithGuid()
    {
        // Arrange
        var dto = BuildValidDto();

        // Act
        var result = await _manager.CreateInvoice(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateInvoice_CallsFluentValidator()
    {
        // Arrange
        var dto = BuildValidDto();

        // Act
        await _manager.CreateInvoice(dto);

        // Assert
        MockValidator.Verify(v => v.ValidateAndThrowAsync(dto), Times.Once);
    }

    [Fact]
    public async Task CreateInvoice_CalculatesLineTotalsCorrectly()
    {
        // Arrange
        var dto = new CreateEInvoiceDto
        {
            InvoiceType = EInvoiceType.EArsiv,
            CustomerTaxId = "12345678901",
            CustomerTitle = "Test Kisi",
            IssueDate = DateTimeOffset.UtcNow,
            Lines = [new CreateEInvoiceLineDto("Urun", 3, 100m, 20)]
        };

        // Act
        var result = await _manager.CreateInvoice(dto);

        // Assert
        result.Success.Should().BeTrue();
        // 3 * 100 = 300 subtotal, 300 * 20/100 = 60 tax, 360 grand total
        mockIntegrationDbContext.Verify(x => x.EInvoices.Add(
            It.Is<EInvoice>(inv =>
                inv.TotalAmount == 300m &&
                inv.TaxAmount == 60m &&
                inv.GrandTotal == 360m)), Times.Once);
    }

    [Fact]
    public async Task CreateInvoice_SetsStatusToDraft()
    {
        // Arrange
        var dto = BuildValidDto();

        // Act
        await _manager.CreateInvoice(dto);

        // Assert
        mockIntegrationDbContext.Verify(x => x.EInvoices.Add(
            It.Is<EInvoice>(inv => inv.Status == EInvoiceStatus.Draft)), Times.Once);
    }

    [Fact]
    public async Task CreateInvoice_GeneratesXmlContent()
    {
        // Arrange
        var dto = BuildValidDto();

        // Act
        await _manager.CreateInvoice(dto);

        // Assert
        mockIntegrationDbContext.Verify(x => x.EInvoices.Add(
            It.Is<EInvoice>(inv => !string.IsNullOrEmpty(inv.XmlContent))), Times.Once);
    }

    [Fact]
    public async Task CancelInvoice_NonExistentInvoice_ReturnsError()
    {
        // Arrange & Act
        var result = await _manager.CancelInvoice(Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadi");
    }

    [Fact]
    public async Task SendToGib_NonExistentInvoice_ReturnsError()
    {
        // Arrange & Act
        var result = await _manager.SendToGib(Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadi");
    }

    [Fact]
    public async Task GetInvoiceDetail_NonExistentInvoice_ReturnsError()
    {
        // Arrange & Act
        var result = await _manager.GetInvoiceDetail(Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateBulkInvoices_MultipleDtos_ReturnsAllIds()
    {
        // Arrange
        var bulkDto = new BulkInvoiceDto([BuildValidDto(), BuildValidDto()]);

        // Act
        var result = await _manager.CreateBulkInvoices(bulkDto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetInvoiceFromSale_NonExistentSale_ReturnsError()
    {
        // Arrange & Act
        var result = await _manager.GetInvoiceFromSale(Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadi");
    }
}
