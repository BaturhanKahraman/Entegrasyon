using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.POS;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Entegrasyon.UnitTest.Business;

public class BranchOfficeManagerTests : BaseTest
{
    private readonly BranchOfficeManager _sut;

    public BranchOfficeManagerTests()
    {
        // Faz 3: validator'lar injection oldu. Delete testleri validator'ları kullanmaz,
        // Mock default (no-op) davranışı yeterli.
        var mockAddValidator = new Mock<IValidator<BranchOfficeAddDto>>();
        var mockEditValidator = new Mock<IValidator<BranchOfficeEditDto>>();

        _sut = new BranchOfficeManager(
            mockContextFactory.Object,
            mockApplicationLogger.Object,
            new BranchOfficeMapper(),
            mockAddValidator.Object,
            mockEditValidator.Object);
    }

    // ---- Soft Delete ----

    [Fact]
    public async Task Delete_ShouldSetIsDeleted_InsteadOfRemove()
    {
        // Arrange: 2 active branches so the "last branch" check passes
        var branches = new List<BranchOffice>
        {
            new() { Id = 1, Name = "Depo 1", IsDeleted = false, IsDefaultMarketPlaceStock = false },
            new() { Id = 2, Name = "Depo 2", IsDeleted = false, IsDefaultMarketPlaceStock = false },
        };
        mockIntegrationDbContext
            .Setup(x => x.BranchOffices)
            .ReturnsDbSet(branches);

        mockIntegrationDbContext
            .Setup(x => x.POSSessions)
            .ReturnsDbSet(new List<POSSession>());

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.Delete(1);

        // Assert
        result.Success.Should().BeTrue();
        // Remove() should NOT have been called
        mockIntegrationDbContext.Verify(x => x.BranchOffices.Remove(It.IsAny<BranchOffice>()), Times.Never);
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_ShouldFail_WhenLastActiveBranch()
    {
        // Arrange: only 1 active branch
        var branches = new List<BranchOffice>
        {
            new() { Id = 1, Name = "Tek Depo", IsDeleted = false, IsDefaultMarketPlaceStock = false },
        };
        mockIntegrationDbContext
            .Setup(x => x.BranchOffices)
            .ReturnsDbSet(branches);

        mockIntegrationDbContext
            .Setup(x => x.POSSessions)
            .ReturnsDbSet(new List<POSSession>());

        // Act
        var result = await _sut.Delete(1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("En az 1 aktif depo");
    }

    [Fact]
    public async Task Delete_ShouldFail_WhenActivePOSSessionExists()
    {
        // Arrange: 2 branches, but branch 1 has an open POS session
        var branches = new List<BranchOffice>
        {
            new() { Id = 1, Name = "Depo 1", IsDeleted = false, IsDefaultMarketPlaceStock = false },
            new() { Id = 2, Name = "Depo 2", IsDeleted = false, IsDefaultMarketPlaceStock = false },
        };
        mockIntegrationDbContext
            .Setup(x => x.BranchOffices)
            .ReturnsDbSet(branches);

        var posSessions = new List<POSSession>
        {
            new() { Id = 1, BranchOfficeId = 1, Status = POSSessionStatus.Open }
        };
        mockIntegrationDbContext
            .Setup(x => x.POSSessions)
            .ReturnsDbSet(posSessions);

        // Act
        var result = await _sut.Delete(1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("POS");
    }

    [Fact]
    public async Task Delete_ShouldFail_WhenIsDefaultMarketPlaceStock()
    {
        // Arrange: 2 branches, branch 1 is default marketplace stock
        var branches = new List<BranchOffice>
        {
            new() { Id = 1, Name = "Varsayilan Depo", IsDeleted = false, IsDefaultMarketPlaceStock = true },
            new() { Id = 2, Name = "Diger Depo", IsDeleted = false, IsDefaultMarketPlaceStock = false },
        };
        mockIntegrationDbContext
            .Setup(x => x.BranchOffices)
            .ReturnsDbSet(branches);

        mockIntegrationDbContext
            .Setup(x => x.POSSessions)
            .ReturnsDbSet(new List<POSSession>());

        // Act
        var result = await _sut.Delete(1);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("varsayilan");
    }

    [Fact]
    public async Task Delete_ShouldSucceed_WhenAllChecksPass()
    {
        // Arrange: 2 branches, no POS, not default
        var branches = new List<BranchOffice>
        {
            new() { Id = 1, Name = "Silinecek Depo", IsDeleted = false, IsDefaultMarketPlaceStock = false },
            new() { Id = 2, Name = "Kalan Depo", IsDeleted = false, IsDefaultMarketPlaceStock = false },
        };
        mockIntegrationDbContext
            .Setup(x => x.BranchOffices)
            .ReturnsDbSet(branches);

        mockIntegrationDbContext
            .Setup(x => x.POSSessions)
            .ReturnsDbSet(new List<POSSession>());

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.Delete(1);

        // Assert
        result.Success.Should().BeTrue();
    }
}
