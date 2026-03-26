using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Storefront;

public class StorefrontFaz3Batch2Tests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory = new();
    private readonly Mock<IntegrationDbContext> _mockDbContext;

    public StorefrontFaz3Batch2Tests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);
    }

    // ============================
    // ExternalLogin Tests
    // ============================

    [Fact]
    public async Task ExternalLoginAsync_NewUser_CreatesAccountAndReturnsSuccess()
    {
        // Arrange
        _mockDbContext.Setup(x => x.StorefrontCustomerAuths)
            .ReturnsDbSet(new List<StorefrontCustomerAuth>());
        _mockDbContext.Setup(x => x.Customers)
            .ReturnsDbSet(new List<Customer>());
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var manager = new StorefrontAuthManager(_mockContextFactory.Object);

        // Act
        var result = await manager.ExternalLoginAsync(1, "Google", "google-123", "ali@test.com", "Ali", "Yilmaz");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Email.Should().Be("ali@test.com");
        result.Data.ExternalLoginProvider.Should().Be("Google");
        result.Data.ExternalLoginId.Should().Be("google-123");
        result.Data.EmailConfirmed.Should().BeTrue();
        result.Data.PasswordHash.Should().BeEmpty();
    }

    [Fact]
    public async Task ExternalLoginAsync_ExistingUser_LinksProviderAndReturnsSuccess()
    {
        // Arrange
        var existing = new StorefrontCustomerAuth
        {
            Id = 1,
            TenantId = 1,
            CustomerId = 10,
            Email = "ali@test.com",
            PasswordHash = new byte[64],
            PasswordSalt = new byte[128],
            EmailConfirmed = false,
            Customer = new RetailCustomer { Id = 10, Name = "Ali", Surname = "Yilmaz", CustomerType = "Retail", Address = new Address() }
        };
        _mockDbContext.Setup(x => x.StorefrontCustomerAuths)
            .ReturnsDbSet(new List<StorefrontCustomerAuth> { existing });
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var manager = new StorefrontAuthManager(_mockContextFactory.Object);

        // Act
        var result = await manager.ExternalLoginAsync(1, "Facebook", "fb-456", "ali@test.com", "Ali", "Yilmaz");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.ExternalLoginProvider.Should().Be("Facebook");
        result.Data.ExternalLoginId.Should().Be("fb-456");
        result.Data.EmailConfirmed.Should().BeTrue();
    }

    // ============================
    // PushManager Tests
    // ============================

    [Fact]
    public async Task SubscribeAsync_NewEndpoint_CreatesSubscription()
    {
        // Arrange
        _mockDbContext.Setup(x => x.StorefrontPushSubscriptions)
            .ReturnsDbSet(new List<StorefrontPushSubscription>());
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var manager = new StorefrontPushManager(_mockContextFactory.Object);

        // Act
        var result = await manager.SubscribeAsync(1, 10, "https://push.example.com/sub1", "p256dh-key", "auth-key");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("kaydedildi");
    }

    [Fact]
    public async Task SubscribeAsync_DuplicateEndpoint_UpdatesExisting()
    {
        // Arrange
        var existing = new StorefrontPushSubscription
        {
            Id = 1,
            TenantId = 1,
            CustomerId = 10,
            Endpoint = "https://push.example.com/sub1",
            P256dhKey = "old-key",
            AuthKey = "old-auth"
        };
        _mockDbContext.Setup(x => x.StorefrontPushSubscriptions)
            .ReturnsDbSet(new List<StorefrontPushSubscription> { existing });
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var manager = new StorefrontPushManager(_mockContextFactory.Object);

        // Act
        var result = await manager.SubscribeAsync(1, 10, "https://push.example.com/sub1", "new-key", "new-auth");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("guncellendi");
    }

    [Fact]
    public async Task UnsubscribeAsync_ExistingEndpoint_RemovesSubscription()
    {
        // Arrange
        var existing = new StorefrontPushSubscription
        {
            Id = 1,
            TenantId = 1,
            Endpoint = "https://push.example.com/sub1",
            P256dhKey = "key",
            AuthKey = "auth"
        };
        _mockDbContext.Setup(x => x.StorefrontPushSubscriptions)
            .ReturnsDbSet(new List<StorefrontPushSubscription> { existing });
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var manager = new StorefrontPushManager(_mockContextFactory.Object);

        // Act
        var result = await manager.UnsubscribeAsync("https://push.example.com/sub1");

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UnsubscribeAsync_NonexistentEndpoint_ReturnsError()
    {
        // Arrange
        _mockDbContext.Setup(x => x.StorefrontPushSubscriptions)
            .ReturnsDbSet(new List<StorefrontPushSubscription>());

        var manager = new StorefrontPushManager(_mockContextFactory.Object);

        // Act
        var result = await manager.UnsubscribeAsync("https://push.example.com/nonexistent");

        // Assert
        result.Success.Should().BeFalse();
    }

    // ============================
    // CampaignManager Tests
    // ============================

    [Fact]
    public async Task CreateCampaignAsync_ValidData_CreatesDraftCampaign()
    {
        // Arrange
        _mockDbContext.Setup(x => x.StorefrontEmailCampaigns)
            .ReturnsDbSet(new List<StorefrontEmailCampaign>());
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var manager = new StorefrontCampaignManager(_mockContextFactory.Object);

        // Act
        var result = await manager.CreateCampaignAsync(1, "Yaz Indirimi", "<h1>Yaz firsatlari</h1>", CampaignTarget.AllSubscribers);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Subject.Should().Be("Yaz Indirimi");
        result.Data.Status.Should().Be(CampaignStatus.Draft);
        result.Data.Target.Should().Be(CampaignTarget.AllSubscribers);
    }

    [Fact]
    public async Task CreateCampaignAsync_EmptySubject_ReturnsError()
    {
        // Arrange
        var manager = new StorefrontCampaignManager(_mockContextFactory.Object);

        // Act
        var result = await manager.CreateCampaignAsync(1, "", "<h1>Content</h1>", CampaignTarget.AllSubscribers);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Konu");
    }

    [Fact]
    public async Task UpdateCampaignAsync_DraftCampaign_UpdatesSuccessfully()
    {
        // Arrange
        var campaign = new StorefrontEmailCampaign
        {
            Id = 1,
            TenantId = 1,
            Subject = "Old",
            HtmlContent = "<p>old</p>",
            Status = CampaignStatus.Draft,
            Target = CampaignTarget.AllSubscribers
        };
        _mockDbContext.Setup(x => x.StorefrontEmailCampaigns)
            .ReturnsDbSet(new List<StorefrontEmailCampaign> { campaign });
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var manager = new StorefrontCampaignManager(_mockContextFactory.Object);

        // Act
        var result = await manager.UpdateCampaignAsync(1, "New Subject", "<p>new</p>", CampaignTarget.ActiveCustomers);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCampaignAsync_SentCampaign_ReturnsError()
    {
        // Arrange
        var campaign = new StorefrontEmailCampaign
        {
            Id = 1,
            TenantId = 1,
            Subject = "Sent",
            HtmlContent = "<p>sent</p>",
            Status = CampaignStatus.Sent,
            Target = CampaignTarget.AllSubscribers
        };
        _mockDbContext.Setup(x => x.StorefrontEmailCampaigns)
            .ReturnsDbSet(new List<StorefrontEmailCampaign> { campaign });

        var manager = new StorefrontCampaignManager(_mockContextFactory.Object);

        // Act
        var result = await manager.UpdateCampaignAsync(1, "New", "<p>new</p>", CampaignTarget.AllCustomers);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("taslak");
    }

    [Fact]
    public async Task ScheduleCampaignAsync_DraftWithFutureDate_SchedulesCampaign()
    {
        // Arrange
        var campaign = new StorefrontEmailCampaign
        {
            Id = 1,
            TenantId = 1,
            Subject = "Test",
            HtmlContent = "<p>test</p>",
            Status = CampaignStatus.Draft,
            Target = CampaignTarget.AllSubscribers
        };
        _mockDbContext.Setup(x => x.StorefrontEmailCampaigns)
            .ReturnsDbSet(new List<StorefrontEmailCampaign> { campaign });
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var manager = new StorefrontCampaignManager(_mockContextFactory.Object);
        var futureDate = DateTimeOffset.UtcNow.AddDays(1);

        // Act
        var result = await manager.ScheduleCampaignAsync(1, futureDate);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ScheduleCampaignAsync_PastDate_ReturnsError()
    {
        // Arrange
        var campaign = new StorefrontEmailCampaign
        {
            Id = 1,
            TenantId = 1,
            Subject = "Test",
            HtmlContent = "<p>test</p>",
            Status = CampaignStatus.Draft,
            Target = CampaignTarget.AllSubscribers
        };
        _mockDbContext.Setup(x => x.StorefrontEmailCampaigns)
            .ReturnsDbSet(new List<StorefrontEmailCampaign> { campaign });

        var manager = new StorefrontCampaignManager(_mockContextFactory.Object);
        var pastDate = DateTimeOffset.UtcNow.AddDays(-1);

        // Act
        var result = await manager.ScheduleCampaignAsync(1, pastDate);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CancelCampaignAsync_ScheduledCampaign_CancelsSuccessfully()
    {
        // Arrange
        var campaign = new StorefrontEmailCampaign
        {
            Id = 1,
            TenantId = 1,
            Subject = "Test",
            HtmlContent = "<p>test</p>",
            Status = CampaignStatus.Scheduled,
            Target = CampaignTarget.AllSubscribers
        };
        _mockDbContext.Setup(x => x.StorefrontEmailCampaigns)
            .ReturnsDbSet(new List<StorefrontEmailCampaign> { campaign });
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var manager = new StorefrontCampaignManager(_mockContextFactory.Object);

        // Act
        var result = await manager.CancelCampaignAsync(1);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CancelCampaignAsync_SentCampaign_ReturnsError()
    {
        // Arrange
        var campaign = new StorefrontEmailCampaign
        {
            Id = 1,
            TenantId = 1,
            Subject = "Test",
            HtmlContent = "<p>test</p>",
            Status = CampaignStatus.Sent,
            Target = CampaignTarget.AllSubscribers
        };
        _mockDbContext.Setup(x => x.StorefrontEmailCampaigns)
            .ReturnsDbSet(new List<StorefrontEmailCampaign> { campaign });

        var manager = new StorefrontCampaignManager(_mockContextFactory.Object);

        // Act
        var result = await manager.CancelCampaignAsync(1);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteCampaignAsync_DraftCampaign_SoftDeletes()
    {
        // Arrange
        var campaign = new StorefrontEmailCampaign
        {
            Id = 1,
            TenantId = 1,
            Subject = "Test",
            HtmlContent = "<p>test</p>",
            Status = CampaignStatus.Draft,
            Target = CampaignTarget.AllSubscribers
        };
        _mockDbContext.Setup(x => x.StorefrontEmailCampaigns)
            .ReturnsDbSet(new List<StorefrontEmailCampaign> { campaign });
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var manager = new StorefrontCampaignManager(_mockContextFactory.Object);

        // Act
        var result = await manager.DeleteCampaignAsync(1);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteCampaignAsync_SendingCampaign_ReturnsError()
    {
        // Arrange
        var campaign = new StorefrontEmailCampaign
        {
            Id = 1,
            TenantId = 1,
            Subject = "Test",
            HtmlContent = "<p>test</p>",
            Status = CampaignStatus.Sending,
            Target = CampaignTarget.AllSubscribers
        };
        _mockDbContext.Setup(x => x.StorefrontEmailCampaigns)
            .ReturnsDbSet(new List<StorefrontEmailCampaign> { campaign });

        var manager = new StorefrontCampaignManager(_mockContextFactory.Object);

        // Act
        var result = await manager.DeleteCampaignAsync(1);

        // Assert
        result.Success.Should().BeFalse();
    }
}
