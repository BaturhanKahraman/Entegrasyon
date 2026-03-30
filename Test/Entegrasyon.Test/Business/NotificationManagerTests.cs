using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Notifications;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Notifications;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Notifications;
using Entegrasyon.Entity.User;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Business;

public class NotificationManagerTests : BaseTest
{
    private readonly NotificationManager _sut;
    private readonly Mock<INotificationSender> _mockSender1 = new();
    private readonly Mock<INotificationSender> _mockSender2 = new();
    private readonly Mock<ILogger<NotificationManager>> _mockLogger = new();
    private readonly EventChannel<NotificationEvent> _eventChannel = new();

    private readonly Guid _userId1 = Guid.NewGuid();
    private readonly Guid _userId2 = Guid.NewGuid();

    public NotificationManagerTests()
    {
        MockValidator = new Mock<IFluentValidator>();
        MockValidator
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<SendNotificationRequest>()))
            .Returns(Task.CompletedTask);

        _mockSender1.Setup(s => s.Type).Returns(SenderType.SignalR);
        _mockSender2.Setup(s => s.Type).Returns(SenderType.Email);

        mockIntegrationDbContext
            .Setup(x => x.Users)
            .ReturnsDbSet(new List<ApplicationUser>
            {
                new() { Id = _userId1, Name = "Test", Surname = "User1" },
                new() { Id = _userId2, Name = "Test", Surname = "User2" }
            });

        mockIntegrationDbContext
            .Setup(x => x.Notifications)
            .ReturnsDbSet(new List<Notification>());

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _sut = new NotificationManager(
            new[] { _mockSender1.Object, _mockSender2.Object },
            mockContextFactory.Object,
            MockValidator.Object,
            _eventChannel,
            mockTenantContext.Object,
            _mockLogger.Object);
    }

    #region SendNotification

    [Fact]
    public async Task SendNotification_ValidRequest_CallsValidatorAndSavesToDb()
    {
        // Arrange
        var header = "Test Header";
        var content = "Test Content";
        var userIds = new List<Guid> { _userId1 };

        // Act
        await _sut.SendNotification(header, content, NotificationSeverity.Info, NotificationCategory.Sistem, userIds);

        // Assert
        MockValidator.Verify(
            v => v.ValidateAndThrowAsync(It.Is<SendNotificationRequest>(r =>
                r.Header == header && r.Content == content)),
            Times.Once);

        mockIntegrationDbContext.Verify(x => x.Notifications.Add(It.Is<Notification>(n =>
            n.Header == header &&
            n.Content == content &&
            n.Severity == NotificationSeverity.Info &&
            n.Category == NotificationCategory.Sistem)),
            Times.Once);

        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendNotification_CallsAllSenders()
    {
        // Arrange
        var userIds = new List<Guid> { _userId1 };

        // Act
        await _sut.SendNotification("H", "C", NotificationSeverity.Info, NotificationCategory.Sistem, userIds);

        // Assert
        _mockSender1.Verify(s => s.SendNotification(It.IsAny<Notification>(), It.IsAny<IEnumerable<Guid>>()), Times.Once);
        _mockSender2.Verify(s => s.SendNotification(It.IsAny<Notification>(), It.IsAny<IEnumerable<Guid>>()), Times.Once);
    }

    [Fact]
    public async Task SendNotification_FirstSenderThrows_SecondSenderStillCalled()
    {
        // Arrange
        _mockSender1
            .Setup(s => s.SendNotification(It.IsAny<Notification>(), It.IsAny<IEnumerable<Guid>>()))
            .ThrowsAsync(new Exception("SignalR connection failed"));

        var userIds = new List<Guid> { _userId1 };

        // Act
        await _sut.SendNotification("H", "C", NotificationSeverity.Error, NotificationCategory.Pazaryeri, userIds);

        // Assert - second sender must still be invoked despite first failure
        _mockSender2.Verify(s => s.SendNotification(It.IsAny<Notification>(), It.IsAny<IEnumerable<Guid>>()), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendNotification_PublishesEventToChannel()
    {
        // Arrange
        var userIds = new List<Guid> { _userId1 };

        // Act
        await _sut.SendNotification("Header", "Content", NotificationSeverity.Warning, NotificationCategory.Siparis, userIds, "/orders/123");

        // Assert
        _eventChannel.Reader.TryRead(out var evt).Should().BeTrue();
        evt.Should().NotBeNull();
        evt!.Header.Should().Be("Header");
        evt.Content.Should().Be("Content");
        evt.Severity.Should().Be(NotificationSeverity.Warning);
        evt.Category.Should().Be(NotificationCategory.Siparis);
        evt.ActionUrl.Should().Be("/orders/123");
        evt.TenantId.Should().Be(1);
    }

    [Fact]
    public async Task SendNotification_WithActionUrl_SetsActionUrlOnNotification()
    {
        // Arrange
        var userIds = new List<Guid> { _userId1 };
        var actionUrl = "/products/42";

        // Act
        await _sut.SendNotification("H", "C", NotificationSeverity.Success, NotificationCategory.Stok, userIds, actionUrl);

        // Assert
        mockIntegrationDbContext.Verify(x => x.Notifications.Add(It.Is<Notification>(n =>
            n.ActionUrl == actionUrl)),
            Times.Once);
    }

    [Fact]
    public async Task SendNotification_OnlyCreatesNotificationsUsersForExistingUsers()
    {
        // Arrange - include a non-existing user ID
        var nonExistentUserId = Guid.NewGuid();
        var userIds = new List<Guid> { _userId1, nonExistentUserId };

        // Act
        await _sut.SendNotification("H", "C", NotificationSeverity.Info, NotificationCategory.Sistem, userIds);

        // Assert - only _userId1 should be in the NotificationsUsers collection
        mockIntegrationDbContext.Verify(x => x.Notifications.Add(It.Is<Notification>(n =>
            n.NotificationsUsers.Count == 1 &&
            n.NotificationsUsers.First().ApplicationUserId == _userId1)),
            Times.Once);
    }

    [Fact]
    public async Task SendNotification_ValidationFails_ThrowsAndDoesNotSave()
    {
        // Arrange
        MockValidator
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<SendNotificationRequest>()))
            .ThrowsAsync(new FluentValidation.ValidationException("Validation failed"));

        var userIds = new List<Guid> { _userId1 };

        // Act
        var act = () => _sut.SendNotification("", "", NotificationSeverity.Info, NotificationCategory.Sistem, userIds);

        // Assert
        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendNotification_MultipleUsers_CreatesNotificationsUsersForEach()
    {
        // Arrange
        var userIds = new List<Guid> { _userId1, _userId2 };

        // Act
        await _sut.SendNotification("H", "C", NotificationSeverity.Info, NotificationCategory.Sistem, userIds);

        // Assert
        mockIntegrationDbContext.Verify(x => x.Notifications.Add(It.Is<Notification>(n =>
            n.NotificationsUsers.Count == 2)),
            Times.Once);
    }

    #endregion

    #region GetNotificationsForUser

    [Fact]
    public async Task GetNotificationsForUser_ExcludesDismissedNotifications()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new()
            {
                Id = 1, Header = "Active", Content = "C1",
                NotificationsUsers = new List<NotificationsUsers>
                {
                    new() { ApplicationUserId = _userId1, IsDismissed = false }
                }
            },
            new()
            {
                Id = 2, Header = "Dismissed", Content = "C2",
                NotificationsUsers = new List<NotificationsUsers>
                {
                    new() { ApplicationUserId = _userId1, IsDismissed = true }
                }
            }
        };

        mockIntegrationDbContext
            .Setup(x => x.Notifications)
            .ReturnsDbSet(notifications);

        // Act
        var result = (await _sut.GetNotificationsForUser(_userId1)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Header.Should().Be("Active");
    }

    [Fact]
    public async Task GetNotificationsForUser_OnlyUnread_FiltersReadNotifications()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new()
            {
                Id = 1, Header = "Unread", IsRead = false,
                NotificationsUsers = new List<NotificationsUsers>
                {
                    new() { ApplicationUserId = _userId1, IsDismissed = false }
                }
            },
            new()
            {
                Id = 2, Header = "Read", IsRead = true,
                NotificationsUsers = new List<NotificationsUsers>
                {
                    new() { ApplicationUserId = _userId1, IsDismissed = false }
                }
            }
        };

        mockIntegrationDbContext
            .Setup(x => x.Notifications)
            .ReturnsDbSet(notifications);

        // Act
        var result = (await _sut.GetNotificationsForUser(_userId1, onlyUnread: true)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Header.Should().Be("Unread");
    }

    [Fact]
    public async Task GetNotificationsForUser_WithTake_LimitsResults()
    {
        // Arrange
        var notifications = Enumerable.Range(1, 10).Select(i => new Notification
        {
            Id = i, Header = $"N{i}", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-i),
            NotificationsUsers = new List<NotificationsUsers>
            {
                new() { ApplicationUserId = _userId1, IsDismissed = false }
            }
        }).ToList();

        mockIntegrationDbContext
            .Setup(x => x.Notifications)
            .ReturnsDbSet(notifications);

        // Act
        var result = (await _sut.GetNotificationsForUser(_userId1, take: 3)).ToList();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetNotificationsForUser_ReturnsOnlyUserNotifications()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new()
            {
                Id = 1, Header = "For User1",
                NotificationsUsers = new List<NotificationsUsers>
                {
                    new() { ApplicationUserId = _userId1, IsDismissed = false }
                }
            },
            new()
            {
                Id = 2, Header = "For User2",
                NotificationsUsers = new List<NotificationsUsers>
                {
                    new() { ApplicationUserId = _userId2, IsDismissed = false }
                }
            }
        };

        mockIntegrationDbContext
            .Setup(x => x.Notifications)
            .ReturnsDbSet(notifications);

        // Act
        var result = (await _sut.GetNotificationsForUser(_userId1)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Header.Should().Be("For User1");
    }

    #endregion

    #region MarkAsRead

    [Fact]
    public async Task MarkAsRead_ExistingNotification_SetsIsReadAndReadAt()
    {
        // Arrange
        var notification = new Notification
        {
            Id = 1, Header = "Test", IsRead = false,
            Users = new List<ApplicationUser> { new() { Id = _userId1 } }
        };

        mockIntegrationDbContext
            .Setup(x => x.Notifications)
            .ReturnsDbSet(new List<Notification> { notification });

        // Act
        await _sut.MarkAsRead(1, _userId1);

        // Assert
        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsRead_NonExistentNotification_DoesNothing()
    {
        // Arrange
        mockIntegrationDbContext
            .Setup(x => x.Notifications)
            .ReturnsDbSet(new List<Notification>());

        // Act
        await _sut.MarkAsRead(999, _userId1);

        // Assert
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkAsRead_NotificationBelongsToOtherUser_DoesNothing()
    {
        // Arrange
        var notification = new Notification
        {
            Id = 1, Header = "Test", IsRead = false,
            Users = new List<ApplicationUser> { new() { Id = _userId2 } }
        };

        mockIntegrationDbContext
            .Setup(x => x.Notifications)
            .ReturnsDbSet(new List<Notification> { notification });

        // Act
        await _sut.MarkAsRead(1, _userId1);

        // Assert
        notification.IsRead.Should().BeFalse();
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GetAllNotificationsAsync

    [Fact]
    public async Task GetAllNotificationsAsync_ReturnsAllNotifications()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new() { Id = 1, Header = "N1", NotificationsUsers = new List<NotificationsUsers>() },
            new() { Id = 2, Header = "N2", NotificationsUsers = new List<NotificationsUsers>() },
            new() { Id = 3, Header = "N3", NotificationsUsers = new List<NotificationsUsers>() }
        };

        mockIntegrationDbContext
            .Setup(x => x.Notifications)
            .ReturnsDbSet(notifications);

        // Act
        var result = await _sut.GetAllNotificationsAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllNotificationsAsync_WithTakeParameter_LimitsResults()
    {
        // Arrange
        var notifications = Enumerable.Range(1, 10).Select(i => new Notification
        {
            Id = i, Header = $"N{i}",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-i),
            NotificationsUsers = new List<NotificationsUsers>()
        }).ToList();

        mockIntegrationDbContext
            .Setup(x => x.Notifications)
            .ReturnsDbSet(notifications);

        // Act
        var result = await _sut.GetAllNotificationsAsync(take: 5);

        // Assert
        result.Should().HaveCount(5);
    }

    #endregion
}
