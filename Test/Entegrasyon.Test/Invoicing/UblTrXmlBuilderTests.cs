using System.Xml.Linq;
using Entegrasyon.Business.Utility.EInvoice;
using Entegrasyon.Entity.Invoicing;

namespace Entegrasyon.UnitTest.Invoicing;

public class UblTrXmlBuilderTests
{
    private static EInvoice BuildTestInvoice(EInvoiceType type = EInvoiceType.EFatura)
    {
        var invoice = new EInvoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "EFT20260001",
            InvoiceType = type,
            CustomerTaxId = "1234567890",
            CustomerTitle = "Test Firma A.S.",
            IssueDate = new DateTimeOffset(2026, 3, 24, 0, 0, 0, TimeSpan.Zero),
            TotalAmount = 200m,
            TaxAmount = 40m,
            GrandTotal = 240m,
            GibUuid = "550e8400-e29b-41d4-a716-446655440000",
            Lines =
            [
                new EInvoiceLine
                {
                    Id = Guid.NewGuid(),
                    ProductName = "Urun A",
                    Quantity = 2,
                    UnitPrice = 100m,
                    TaxRate = 20,
                    TaxAmount = 40m,
                    LineTotal = 240m
                }
            ]
        };
        return invoice;
    }

    [Fact]
    public void Build_ReturnsValidXml()
    {
        // Arrange
        var invoice = BuildTestInvoice();

        // Act
        var xml = UblTrXmlBuilder.Build(invoice);

        // Assert
        xml.Should().NotBeNullOrEmpty();
        xml.Should().Contain("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
    }

    [Fact]
    public void Build_ContainsInvoiceNumber()
    {
        // Arrange
        var invoice = BuildTestInvoice();

        // Act
        var xml = UblTrXmlBuilder.Build(invoice);

        // Assert
        xml.Should().Contain(invoice.InvoiceNumber);
    }

    [Fact]
    public void Build_EFatura_UsesCommercialProfile()
    {
        // Arrange
        var invoice = BuildTestInvoice(EInvoiceType.EFatura);

        // Act
        var xml = UblTrXmlBuilder.Build(invoice);

        // Assert
        xml.Should().Contain("TICARIFATURA");
    }

    [Fact]
    public void Build_EArsiv_UsesArchiveProfile()
    {
        // Arrange
        var invoice = BuildTestInvoice(EInvoiceType.EArsiv);

        // Act
        var xml = UblTrXmlBuilder.Build(invoice);

        // Assert
        xml.Should().Contain("EARSIVFATURA");
    }

    [Fact]
    public void Build_ContainsCustomerTaxIdWithVknScheme()
    {
        // Arrange — 10-digit = VKN
        var invoice = BuildTestInvoice();
        invoice.CustomerTaxId = "1234567890";

        // Act
        var xml = UblTrXmlBuilder.Build(invoice);

        // Assert
        xml.Should().Contain("schemeID=\"VKN\"");
        xml.Should().Contain("1234567890");
    }

    [Fact]
    public void Build_ContainsCustomerTaxIdWithTcknScheme()
    {
        // Arrange — 11-digit = TCKN
        var invoice = BuildTestInvoice();
        invoice.CustomerTaxId = "12345678901";

        // Act
        var xml = UblTrXmlBuilder.Build(invoice);

        // Assert
        xml.Should().Contain("schemeID=\"TCKN\"");
    }

    [Fact]
    public void Build_ContainsLineDetails()
    {
        // Arrange
        var invoice = BuildTestInvoice();

        // Act
        var xml = UblTrXmlBuilder.Build(invoice);

        // Assert
        xml.Should().Contain("Urun A");
        xml.Should().Contain("KDV");
        xml.Should().Contain("0015"); // KDV tax type code
    }

    [Fact]
    public void Build_ContainsCurrencyTry()
    {
        // Arrange
        var invoice = BuildTestInvoice();

        // Act
        var xml = UblTrXmlBuilder.Build(invoice);

        // Assert
        xml.Should().Contain("TRY");
    }
}
