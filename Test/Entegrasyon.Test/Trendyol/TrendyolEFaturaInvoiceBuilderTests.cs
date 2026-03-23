using Entegrasyon.Business.Concrete.Trendyol.EFatura;
using Entegrasyon.Entity.Orders;

namespace Entegrasyon.Test.Trendyol;

/// <summary>
/// TrendyolEFaturaInvoiceBuilder unit testleri.
/// Tutar cevirme, KDV hesaplama, request body olusturma.
/// </summary>
public class TrendyolEFaturaInvoiceBuilderTests
{
    private readonly TrendyolEFaturaInvoiceBuilder _sut = new();

    // ── ToKurus Tests ────────────────────────────────────────────────────

    [Theory]
    [InlineData(114.55, 11455)]
    [InlineData(0, 0)]
    [InlineData(1.00, 100)]
    [InlineData(99.99, 9999)]
    [InlineData(0.01, 1)]
    [InlineData(1234.56, 123456)]
    public void ToKurus_ConvertsDecimalCorrectly(decimal input, long expected)
    {
        var result = TrendyolEFaturaInvoiceBuilder.ToKurus(input);
        result.Should().Be(expected);
    }

    // ── CalculateTaxAmount Tests ─────────────────────────────────────────

    [Theory]
    [InlineData(11800, 18, 1800)]   // 100 TL + 18% KDV = 118 TL → KDV = 18 TL = 1800 kurus
    [InlineData(11000, 10, 1000)]   // 100 TL + 10% KDV = 110 TL → KDV = 10 TL = 1000 kurus
    [InlineData(10000, 0, 0)]       // KDV yok
    [InlineData(0, 18, 0)]          // Tutar sifir
    public void CalculateTaxAmount_ReturnsCorrectKdv(long taxInclusiveKurus, int vatRate, long expectedTaxKurus)
    {
        var result = TrendyolEFaturaInvoiceBuilder.CalculateTaxAmount(taxInclusiveKurus, vatRate);
        result.Should().Be(expectedTaxKurus);
    }

    [Fact]
    public void CalculateTaxAmount_NegativeVatRate_ReturnsZero()
    {
        var result = TrendyolEFaturaInvoiceBuilder.CalculateTaxAmount(10000, -5);
        result.Should().Be(0);
    }

    // ── BuildFromOrder Tests ─────────────────────────────────────────────

    [Fact]
    public void BuildFromOrder_EArchive_SetsCorrectInvoiceType()
    {
        var order = CreateTestOrder();

        var result = _sut.BuildFromOrder(order, companyId: 100, userId: 200, isEInvoice: false);

        result.InvoiceInfo.InvoiceType.Should().Be("EARSIVFATURA");
        result.InvoiceInfo.InvoiceTypeCode.Should().Be("SATIS");
        result.Source.Should().Be("PARTNER");
        result.TargetAlias.Should().BeNull();
    }

    [Fact]
    public void BuildFromOrder_EInvoice_SetsCorrectInvoiceType()
    {
        var order = CreateTestOrder();

        var result = _sut.BuildFromOrder(order, companyId: 100, userId: 200, isEInvoice: true, targetAlias: "urn:mail:test@test.com");

        result.InvoiceInfo.InvoiceType.Should().Be("TEMELFATURA");
        result.TargetAlias.Should().Be("urn:mail:test@test.com");
    }

    [Fact]
    public void BuildFromOrder_SetsCompanyIdAndUserId()
    {
        var order = CreateTestOrder();

        var result = _sut.BuildFromOrder(order, companyId: 42, userId: 99, isEInvoice: false);

        result.CompanyId.Should().Be(42);
        result.UserId.Should().Be(99);
    }

    [Fact]
    public void BuildFromOrder_ConvertsAmountsToKurus()
    {
        var order = CreateTestOrder(unitPrice: 100m, quantity: 2, vatRate: 18);

        var result = _sut.BuildFromOrder(order, companyId: 1, userId: 1, isEInvoice: false);

        // 100 TL * 2 adet = 200 TL = 20000 kurus (KDV dahil)
        result.InvoiceTotal.PayableAmount.Should().Be(20000);
        result.InvoiceTotal.TaxInclusiveAmount.Should().Be(20000);

        // KDV: 20000 - (20000 * 100 / 118) = 20000 - 16949 = 3051 kurus
        result.TotalTax.TotalTaxAmount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void BuildFromOrder_MultipleItems_SumsCorrectly()
    {
        var order = CreateTestOrderWithMultipleItems();

        var result = _sut.BuildFromOrder(order, companyId: 1, userId: 1, isEInvoice: false);

        result.InvoiceLines.Should().HaveCount(2);
        result.InvoiceTotal.PayableAmount.Should().Be(
            result.InvoiceLines.Sum(l => l.TotalAmount));
    }

    [Fact]
    public void BuildFromOrder_PaymentInfoIsSet()
    {
        var order = CreateTestOrder();

        var result = _sut.BuildFromOrder(order, companyId: 1, userId: 1, isEInvoice: false);

        result.PaymentInfo.Should().NotBeNull();
        result.PaymentInfo!.PaymentMeans.Should().Be("CREDIT_CARD");
    }

    [Fact]
    public void BuildFromOrder_DeliveryInfoIsSet()
    {
        var order = CreateTestOrder();
        order.CargoProviderName = "Aras Kargo";

        var result = _sut.BuildFromOrder(order, companyId: 1, userId: 1, isEInvoice: false);

        result.DeliveryInfo.Should().NotBeNull();
        result.DeliveryInfo!.CarrierName.Should().Be("Aras Kargo");
    }

    [Fact]
    public void BuildFromOrder_OrderInfoIsSet()
    {
        var order = CreateTestOrder();
        order.OrderNumber = "TY-12345";

        var result = _sut.BuildFromOrder(order, companyId: 1, userId: 1, isEInvoice: false);

        result.OrderInfo.Should().NotBeNull();
        result.OrderInfo!.OrderId.Should().Be("TY-12345");
    }

    [Fact]
    public void BuildFromOrder_RecipientInfoFromBillingAddress()
    {
        var order = CreateTestOrder();
        order.CustomerFirstName = "Ali";
        order.CustomerLastName = "Yilmaz";
        order.CustomerEmail = "ali@test.com";
        order.BillingAddress = new Entity.Address
        {
            City = "Istanbul",
            County = "Kadikoy",
            FullAddress = "Test Adres",
            ZipCode = "34000",
            Country = "TR",
            Street = ""
        };

        var result = _sut.BuildFromOrder(order, companyId: 1, userId: 1, isEInvoice: false);

        result.RecipientInfo.Name.Should().Be("Ali");
        result.RecipientInfo.Surname.Should().Be("Yilmaz");
        result.RecipientInfo.Email.Should().Be("ali@test.com");
        result.RecipientInfo.City.Should().Be("Istanbul");
        result.RecipientInfo.District.Should().Be("Kadikoy");
    }

    [Fact]
    public void BuildFromOrder_LocalReferenceIdContainsOrderNumber()
    {
        var order = CreateTestOrder();
        order.OrderNumber = "TY-99999";

        var result = _sut.BuildFromOrder(order, companyId: 1, userId: 1, isEInvoice: false);

        result.LocalReferenceId.Should().Contain("TY-99999");
    }

    [Fact]
    public void BuildFromOrder_DiscountApplied_ReducesTotalAmount()
    {
        var order = CreateTestOrder(unitPrice: 100m, quantity: 1, vatRate: 18, discount: 10m);

        var result = _sut.BuildFromOrder(order, companyId: 1, userId: 1, isEInvoice: false);

        // 100 TL - 10 TL indirim = 90 TL = 9000 kurus
        result.InvoiceLines[0].TotalAmount.Should().Be(9000);
        result.InvoiceLines[0].TotalDiscountAmount.Should().Be(1000);
    }

    // ── Helper Methods ───────────────────────────────────────────────────

    private static Order CreateTestOrder(decimal unitPrice = 50m, int quantity = 1, int vatRate = 18, decimal discount = 0m)
    {
        var orderId = Guid.NewGuid();
        return new Order
        {
            Id = orderId,
            OrderNumber = "TEST-001",
            OrderDate = DateTimeOffset.UtcNow.AddDays(-1),
            CustomerFirstName = "Test",
            CustomerLastName = "User",
            CustomerEmail = "test@test.com",
            BillingAddress = new Entity.Address
            {
                City = "Istanbul",
                County = "Besiktas",
                FullAddress = "Test Adres 1",
                ZipCode = "34000",
                Country = "TR",
                Street = ""
            },
            ShippingAddress = new Entity.Address
            {
                City = "Istanbul",
                County = "Besiktas",
                FullAddress = "Test Adres 1",
                ZipCode = "34000",
                Country = "TR",
                Street = ""
            },
            OrderItems = new List<OrderItem>
            {
                new()
                {
                    Id = 1,
                    OrderId = orderId,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    VatRate = vatRate,
                    Barcode = "TEST-BARCODE-001",
                    Discount = discount
                }
            }
        };
    }

    private static Order CreateTestOrderWithMultipleItems()
    {
        var orderId = Guid.NewGuid();
        return new Order
        {
            Id = orderId,
            OrderNumber = "TEST-MULTI",
            OrderDate = DateTimeOffset.UtcNow.AddDays(-1),
            BillingAddress = new Entity.Address { City = "Ankara", County = "Cankaya", FullAddress = "Adres", ZipCode = "06000", Country = "TR", Street = "" },
            ShippingAddress = new Entity.Address { City = "Ankara", County = "Cankaya", FullAddress = "Adres", ZipCode = "06000", Country = "TR", Street = "" },
            OrderItems = new List<OrderItem>
            {
                new() { Id = 1, OrderId = orderId, Quantity = 1, UnitPrice = 100m, VatRate = 18, Barcode = "B1" },
                new() { Id = 2, OrderId = orderId, Quantity = 2, UnitPrice = 50m, VatRate = 10, Barcode = "B2" }
            }
        };
    }
}
