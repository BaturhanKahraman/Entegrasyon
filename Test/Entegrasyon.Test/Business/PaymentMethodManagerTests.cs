using Entegrasyon.Business.Concrete;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Sales;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;

namespace Entegrasyon.UnitTest.Business;

public class PaymentMethodManagerTests : BaseTest
{
    private readonly PaymentMethodManager _sut;

    public PaymentMethodManagerTests()
    {
        _sut = new PaymentMethodManager(mockContextFactory.Object);
    }

    private void SetupPaymentMethods(IEnumerable<PaymentMethodDefinition> methods)
    {
        mockIntegrationDbContext
            .Setup(x => x.PaymentMethodDefinitions)
            .ReturnsDbSet(methods.ToList());

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task GetActivePaymentMethodsAsync_ReturnsOnlyActiveMethodsForTenant()
    {
        // Arrange
        SetupPaymentMethods([
            new() { Id = 1, Name = "Nakit", SystemCode = "CASH", IsActive = true, SortOrder = 1, TenantId = 1 },
            new() { Id = 2, Name = "Kredi Kartı", SystemCode = "CREDIT", IsActive = true, SortOrder = 2, TenantId = 1 },
            new() { Id = 3, Name = "Pasif Yöntem", SystemCode = "INACTIVE", IsActive = false, SortOrder = 3, TenantId = 1 },
            new() { Id = 4, Name = "Başka Kiracı", SystemCode = "OTHER", IsActive = true, SortOrder = 1, TenantId = 2 }
        ]);

        // Act
        var result = await _sut.GetActivePaymentMethodsAsync(tenantId: 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data.Should().OnlyContain(x => x.IsActive && x.TenantId == 1);
    }

    [Fact]
    public async Task GetActivePaymentMethodsAsync_OrdersBySortOrder()
    {
        // Arrange
        SetupPaymentMethods([
            new() { Id = 2, Name = "Kredi Kartı", SystemCode = "CREDIT", IsActive = true, SortOrder = 2, TenantId = 1 },
            new() { Id = 1, Name = "Nakit", SystemCode = "CASH", IsActive = true, SortOrder = 1, TenantId = 1 }
        ]);

        // Act
        var result = await _sut.GetActivePaymentMethodsAsync(tenantId: 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Select(x => x.SortOrder).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetAllPaymentMethodsAsync_ReturnsAllMethodsForTenant()
    {
        // Arrange
        SetupPaymentMethods([
            new() { Id = 1, Name = "Nakit", SystemCode = "CASH", IsActive = true, SortOrder = 1, TenantId = 1 },
            new() { Id = 2, Name = "Pasif Yöntem", SystemCode = "INACTIVE", IsActive = false, SortOrder = 2, TenantId = 1 },
            new() { Id = 3, Name = "Başka Kiracı", SystemCode = "OTHER", IsActive = true, SortOrder = 1, TenantId = 2 }
        ]);

        // Act
        var result = await _sut.GetAllPaymentMethodsAsync(tenantId: 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data.Should().OnlyContain(x => x.TenantId == 1);
    }

    [Fact]
    public async Task TogglePaymentMethodAsync_TogglesIsActive_FromTrueToFalse()
    {
        // Arrange
        var method = new PaymentMethodDefinition { Id = 1, Name = "Nakit", SystemCode = "CASH", IsActive = true, SortOrder = 1, TenantId = 1 };
        SetupPaymentMethods([method]);

        // Act
        var result = await _sut.TogglePaymentMethodAsync(id: 1);

        // Assert
        result.Success.Should().BeTrue();
        method.IsActive.Should().BeFalse();
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TogglePaymentMethodAsync_TogglesIsActive_FromFalseToTrue()
    {
        // Arrange
        var method = new PaymentMethodDefinition { Id = 1, Name = "Nakit", SystemCode = "CASH", IsActive = false, SortOrder = 1, TenantId = 1 };
        SetupPaymentMethods([method]);

        // Act
        var result = await _sut.TogglePaymentMethodAsync(id: 1);

        // Assert
        result.Success.Should().BeTrue();
        method.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task TogglePaymentMethodAsync_ReturnsError_WhenNotFound()
    {
        // Arrange
        SetupPaymentMethods([]);

        // Act
        var result = await _sut.TogglePaymentMethodAsync(id: 999);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadı");
    }

    [Fact]
    public async Task CreatePaymentMethodAsync_WithDuplicateSystemCode_ReturnsError()
    {
        // Arrange — existing method with same SystemCode for same tenant
        SetupPaymentMethods([
            new() { Id = 1, Name = "Nakit", SystemCode = "CASH", IsActive = true, SortOrder = 1, TenantId = 1 }
        ]);

        // Act
        var result = await _sut.CreatePaymentMethodAsync("Nakit 2", "CASH", "cash-icon", false, true, tenantId: 1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("sistem kodu");
    }

    [Fact]
    public async Task CreatePaymentMethodAsync_AssignsNextSortOrder()
    {
        // Arrange — two existing methods with SortOrder 1 and 2
        SetupPaymentMethods([
            new() { Id = 1, Name = "Nakit", SystemCode = "CASH", IsActive = true, SortOrder = 1, TenantId = 1 },
            new() { Id = 2, Name = "Kredi", SystemCode = "CREDIT", IsActive = true, SortOrder = 2, TenantId = 1 }
        ]);

        // Act
        var result = await _sut.CreatePaymentMethodAsync("Havale", "BANK_TRANSFER", "bank-icon", false, false, tenantId: 1);

        // Assert
        result.Success.Should().BeTrue();
        mockIntegrationDbContext.Verify(x => x.PaymentMethodDefinitions.Add(
            It.Is<PaymentMethodDefinition>(m => m.SortOrder == 3 && m.SystemCode == "BANK_TRANSFER")), Times.Once);
    }

    [Fact]
    public async Task CreatePaymentMethodAsync_WithUniqueSystemCode_Succeeds()
    {
        // Arrange
        SetupPaymentMethods([]);

        // Act
        var result = await _sut.CreatePaymentMethodAsync("Nakit", "CASH", "cash-icon", false, true, tenantId: 1);

        // Assert
        result.Success.Should().BeTrue();
        mockIntegrationDbContext.Verify(x => x.PaymentMethodDefinitions.Add(It.IsAny<PaymentMethodDefinition>()), Times.Once);
    }

    [Fact]
    public async Task CreatePaymentMethodAsync_AllowsDuplicateSystemCode_ForDifferentTenants()
    {
        // Arrange — TenantId=1 has CASH, creating for TenantId=2
        SetupPaymentMethods([
            new() { Id = 1, Name = "Nakit", SystemCode = "CASH", IsActive = true, SortOrder = 1, TenantId = 1 }
        ]);

        // Act
        var result = await _sut.CreatePaymentMethodAsync("Nakit", "CASH", "cash-icon", false, true, tenantId: 2);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePaymentMethodAsync_UpdatesNameIconCommission()
    {
        // Arrange
        var method = new PaymentMethodDefinition { Id = 1, Name = "Eski Ad", SystemCode = "CASH", Icon = "old-icon", CommissionRate = null, IsActive = true, SortOrder = 1, TenantId = 1 };
        SetupPaymentMethods([method]);

        // Act
        var result = await _sut.UpdatePaymentMethodAsync(1, "Yeni Ad", "new-icon", 1.5m);

        // Assert
        result.Success.Should().BeTrue();
        method.Name.Should().Be("Yeni Ad");
        method.Icon.Should().Be("new-icon");
        method.CommissionRate.Should().Be(1.5m);
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePaymentMethodAsync_ReturnsError_WhenNotFound()
    {
        // Arrange
        SetupPaymentMethods([]);

        // Act
        var result = await _sut.UpdatePaymentMethodAsync(999, "Ad", "icon", null);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadı");
    }

    [Fact]
    public async Task ReorderPaymentMethodsAsync_UpdatesSortOrders()
    {
        // Arrange
        var method1 = new PaymentMethodDefinition { Id = 1, Name = "Nakit", SystemCode = "CASH", IsActive = true, SortOrder = 1, TenantId = 1 };
        var method2 = new PaymentMethodDefinition { Id = 2, Name = "Kredi", SystemCode = "CREDIT", IsActive = true, SortOrder = 2, TenantId = 1 };
        var method3 = new PaymentMethodDefinition { Id = 3, Name = "Havale", SystemCode = "BANK", IsActive = true, SortOrder = 3, TenantId = 1 };
        SetupPaymentMethods([method1, method2, method3]);

        // Act — reverse the order: 3, 1, 2
        var result = await _sut.ReorderPaymentMethodsAsync([3, 1, 2]);

        // Assert
        result.Success.Should().BeTrue();
        method3.SortOrder.Should().Be(1);
        method1.SortOrder.Should().Be(2);
        method2.SortOrder.Should().Be(3);
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
