using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Chat;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Chat;
using Entegrasyon.Entity.Dtos.Chat;
using Entegrasyon.Entity.User;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Business;

public class ChatManagerTests : BaseTest
{
    private readonly ChatManager _sut;
    private readonly Mock<ILogger<ChatManager>> _mockLogger = new();
    private readonly EventChannel<ChatMessageEvent> _eventChannel = new();

    private readonly Guid _userId1 = Guid.NewGuid();
    private readonly Guid _userId2 = Guid.NewGuid();
    private readonly Guid _userId3 = Guid.NewGuid();

    public ChatManagerTests()
    {
        MockValidator = new Mock<IFluentValidator>();
        MockValidator
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<SendChatMessageDto>()))
            .Returns(Task.CompletedTask);

        mockIntegrationDbContext
            .Setup(x => x.Users)
            .ReturnsDbSet(new List<ApplicationUser>
            {
                new() { Id = _userId1, Name = "Ali", Surname = "Yilmaz", FullName = "Ali Yilmaz", IsActive = true },
                new() { Id = _userId2, Name = "Veli", Surname = "Demir", FullName = "Veli Demir", IsActive = true },
                new() { Id = _userId3, Name = "Ayse", Surname = "Kaya", FullName = "Ayse Kaya", IsActive = false }
            });

        mockIntegrationDbContext
            .Setup(x => x.ChatConversations)
            .ReturnsDbSet(new List<ChatConversation>());

        mockIntegrationDbContext
            .Setup(x => x.ChatMessages)
            .ReturnsDbSet(new List<ChatMessage>());

        mockIntegrationDbContext
            .Setup(x => x.ChatParticipants)
            .ReturnsDbSet(new List<ChatParticipant>());

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _sut = new ChatManager(
            mockContextFactory.Object,
            MockValidator.Object,
            _eventChannel,
            mockTenantContext.Object,
            _mockLogger.Object);
    }

    #region GetOrCreateDirectConversation

    [Fact]
    public async Task GetOrCreateDirectConversation_EmptyUserId1_ThrowsArgumentException()
    {
        // Act
        var act = () => _sut.GetOrCreateDirectConversation(Guid.Empty, _userId2);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*boş olamaz*");
    }

    [Fact]
    public async Task GetOrCreateDirectConversation_EmptyUserId2_ThrowsArgumentException()
    {
        // Act
        var act = () => _sut.GetOrCreateDirectConversation(_userId1, Guid.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*boş olamaz*");
    }

    [Fact]
    public async Task GetOrCreateDirectConversation_SameUser_ThrowsArgumentException()
    {
        // Act
        var act = () => _sut.GetOrCreateDirectConversation(_userId1, _userId1);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Kendisiyle*");
    }

    [Fact]
    public async Task GetOrCreateDirectConversation_ExistingConversation_ReturnsExisting()
    {
        // Arrange
        var existingConversation = new ChatConversation
        {
            Id = 42,
            Type = ChatConversationType.Direct,
            CreatedByUserId = _userId1,
            Participants = new List<ChatParticipant>
            {
                new() { UserId = _userId1, IsRemoved = false },
                new() { UserId = _userId2, IsRemoved = false }
            }
        };

        mockIntegrationDbContext
            .Setup(x => x.ChatConversations)
            .ReturnsDbSet(new List<ChatConversation> { existingConversation });

        // Act
        var result = await _sut.GetOrCreateDirectConversation(_userId1, _userId2);

        // Assert
        result.Id.Should().Be(42);
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateDirectConversation_NewConversation_CreatesWithBothParticipants()
    {
        // Arrange — no existing conversations (default empty setup)

        // Act
        var result = await _sut.GetOrCreateDirectConversation(_userId1, _userId2);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(ChatConversationType.Direct);
        result.CreatedByUserId.Should().Be(_userId1);
        result.Participants.Should().HaveCount(2);
        result.Participants.Should().Contain(p => p.UserId == _userId1);
        result.Participants.Should().Contain(p => p.UserId == _userId2);

        mockIntegrationDbContext.Verify(
            x => x.ChatConversations.Add(It.Is<ChatConversation>(c =>
                c.Type == ChatConversationType.Direct &&
                c.Participants.Count == 2)),
            Times.Once);
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateDirectConversation_RemovedParticipant_CreatesNewConversation()
    {
        // Arrange — existing conversation but one participant is removed
        var existingConversation = new ChatConversation
        {
            Id = 10,
            Type = ChatConversationType.Direct,
            CreatedByUserId = _userId1,
            Participants = new List<ChatParticipant>
            {
                new() { UserId = _userId1, IsRemoved = false },
                new() { UserId = _userId2, IsRemoved = true } // removed
            }
        };

        mockIntegrationDbContext
            .Setup(x => x.ChatConversations)
            .ReturnsDbSet(new List<ChatConversation> { existingConversation });

        // Act
        var result = await _sut.GetOrCreateDirectConversation(_userId1, _userId2);

        // Assert — should create a new conversation since the existing one has a removed participant
        mockIntegrationDbContext.Verify(
            x => x.ChatConversations.Add(It.IsAny<ChatConversation>()),
            Times.Once);
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region SendMessage

    [Fact]
    public async Task SendMessage_ValidRequest_PersistsAndPublishesEvent()
    {
        // Arrange
        var conversationId = 1L;
        var dto = new SendChatMessageDto(conversationId, "Merhaba!", ChatMessageType.Text);

        mockIntegrationDbContext
            .Setup(x => x.ChatParticipants)
            .ReturnsDbSet(new List<ChatParticipant>
            {
                new() { ConversationId = conversationId, UserId = _userId1, IsRemoved = false },
                new() { ConversationId = conversationId, UserId = _userId2, IsRemoved = false }
            });

        // Act
        var result = await _sut.SendMessage(_userId1, dto);

        // Assert
        result.Should().NotBeNull();
        result.ConversationId.Should().Be(conversationId);
        result.SenderId.Should().Be(_userId1);
        result.Content.Should().Be("Merhaba!");
        result.MessageType.Should().Be(ChatMessageType.Text);

        MockValidator.Verify(
            v => v.ValidateAndThrowAsync(It.Is<SendChatMessageDto>(d => d.Content == "Merhaba!")),
            Times.Once);

        mockIntegrationDbContext.Verify(
            x => x.ChatMessages.Add(It.Is<ChatMessage>(m =>
                m.ConversationId == conversationId &&
                m.SenderId == _userId1 &&
                m.Content == "Merhaba!")),
            Times.Once);

        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Event published
        _eventChannel.Reader.TryRead(out var evt).Should().BeTrue();
        evt.Should().NotBeNull();
        evt!.ConversationId.Should().Be(conversationId);
        evt.SenderId.Should().Be(_userId1);
        evt.SenderName.Should().Be("Ali Yilmaz");
        evt.Content.Should().Be("Merhaba!");
        evt.RecipientUserIds.Should().Contain(_userId2);
        evt.RecipientUserIds.Should().NotContain(_userId1);
        evt.TenantId.Should().Be(1);
    }

    [Fact]
    public async Task SendMessage_UserNotParticipant_ThrowsAndDoesNotPublishEvent()
    {
        // Arrange
        var dto = new SendChatMessageDto(1L, "Test", ChatMessageType.Text);

        mockIntegrationDbContext
            .Setup(x => x.ChatParticipants)
            .ReturnsDbSet(new List<ChatParticipant>
            {
                new() { ConversationId = 1L, UserId = _userId2, IsRemoved = false }
                // _userId1 is NOT a participant
            });

        // Act
        var act = () => _sut.SendMessage(_userId1, dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*yetkiniz yok*");

        _eventChannel.Reader.TryRead(out _).Should().BeFalse();
    }

    [Fact]
    public async Task SendMessage_ValidationFails_ThrowsAndDoesNotSave()
    {
        // Arrange
        MockValidator
            .Setup(v => v.ValidateAndThrowAsync(It.IsAny<SendChatMessageDto>()))
            .ThrowsAsync(new FluentValidation.ValidationException("Validation failed"));

        var dto = new SendChatMessageDto(1L, "", ChatMessageType.Text);

        // Act
        var act = () => _sut.SendMessage(_userId1, dto);

        // Assert
        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
        mockIntegrationDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _eventChannel.Reader.TryRead(out _).Should().BeFalse();
    }

    [Fact]
    public async Task SendMessage_WithReplyToMessageId_SetsReplyId()
    {
        // Arrange
        var conversationId = 1L;
        var dto = new SendChatMessageDto(conversationId, "Cevap", ChatMessageType.Text, ReplyToMessageId: 99L);

        mockIntegrationDbContext
            .Setup(x => x.ChatParticipants)
            .ReturnsDbSet(new List<ChatParticipant>
            {
                new() { ConversationId = conversationId, UserId = _userId1, IsRemoved = false }
            });

        // Act
        var result = await _sut.SendMessage(_userId1, dto);

        // Assert
        result.ReplyToMessageId.Should().Be(99L);
    }

    [Fact]
    public async Task SendMessage_RemovedParticipant_ThrowsInvalidOperation()
    {
        // Arrange
        var dto = new SendChatMessageDto(1L, "Test", ChatMessageType.Text);

        mockIntegrationDbContext
            .Setup(x => x.ChatParticipants)
            .ReturnsDbSet(new List<ChatParticipant>
            {
                new() { ConversationId = 1L, UserId = _userId1, IsRemoved = true }
            });

        // Act
        var act = () => _sut.SendMessage(_userId1, dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region GetMessages

    [Fact]
    public async Task GetMessages_UserIsParticipant_ReturnsMessages()
    {
        // Arrange
        var conversationId = 1L;

        mockIntegrationDbContext
            .Setup(x => x.ChatParticipants)
            .ReturnsDbSet(new List<ChatParticipant>
            {
                new() { ConversationId = conversationId, UserId = _userId1, IsRemoved = false, LastReadAt = DateTimeOffset.UtcNow }
            });

        var messages = new List<ChatMessage>
        {
            new()
            {
                Id = 1, ConversationId = conversationId, SenderId = _userId2,
                Content = "Merhaba", MessageType = ChatMessageType.Text,
                Sender = new ApplicationUser { FullName = "Veli Demir" }
            },
            new()
            {
                Id = 2, ConversationId = conversationId, SenderId = _userId1,
                Content = "Selam", MessageType = ChatMessageType.Text,
                Sender = new ApplicationUser { FullName = "Ali Yilmaz" }
            }
        };

        mockIntegrationDbContext
            .Setup(x => x.ChatMessages)
            .ReturnsDbSet(messages);

        // Act
        var result = await _sut.GetMessages(conversationId, _userId1);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMessages_UserNotParticipant_ReturnsEmptyList()
    {
        // Arrange
        mockIntegrationDbContext
            .Setup(x => x.ChatParticipants)
            .ReturnsDbSet(new List<ChatParticipant>
            {
                new() { ConversationId = 1L, UserId = _userId2, IsRemoved = false }
            });

        // Act
        var result = await _sut.GetMessages(1L, _userId1);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMessages_RemovedParticipant_ReturnsEmptyList()
    {
        // Arrange
        mockIntegrationDbContext
            .Setup(x => x.ChatParticipants)
            .ReturnsDbSet(new List<ChatParticipant>
            {
                new() { ConversationId = 1L, UserId = _userId1, IsRemoved = true }
            });

        // Act
        var result = await _sut.GetMessages(1L, _userId1);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetAvailableChatUsers

    [Fact]
    public async Task GetAvailableChatUsers_ExcludesCurrentUser()
    {
        // Act
        var result = await _sut.GetAvailableChatUsers(_userId1);

        // Assert
        result.Should().NotContain(u => u.Id == _userId1);
    }

    [Fact]
    public async Task GetAvailableChatUsers_ExcludesInactiveUsers()
    {
        // Act
        var result = await _sut.GetAvailableChatUsers(_userId1);

        // Assert — _userId3 is inactive
        result.Should().NotContain(u => u.Id == _userId3);
    }

    [Fact]
    public async Task GetAvailableChatUsers_ReturnsOnlyActiveOtherUsers()
    {
        // Act
        var result = await _sut.GetAvailableChatUsers(_userId1);

        // Assert — only _userId2 (Veli) is active and not the current user
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(_userId2);
    }

    #endregion
}
