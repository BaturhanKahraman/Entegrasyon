using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Chat;
using Entegrasyon.Entity.Chat;
using Entegrasyon.Entity.Dtos.Chat;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.Blazor.Features.Chat;

public partial class MessageArea : ComponentBase, IDisposable
{
    [Parameter] public long ConversationId { get; set; }
    [Parameter] public Guid CurrentUserId { get; set; }

    [Inject] private IServiceScopeFactory ScopeFactory { get; set; } = null!;
    [Inject] private IChatDeliveryService DeliveryService { get; set; } = null!;

    private ElementReference _messageContainer;
    private List<ChatMessageDto> _messages = [];
    private string _messageText = string.Empty;
    private string _conversationTitle = string.Empty;
    private bool _loading;
    private bool _sending;
    private long _currentConversationId;

    protected override async Task OnParametersSetAsync()
    {
        if (ConversationId == _currentConversationId)
            return;

        _currentConversationId = ConversationId;
        await LoadMessages();
        await MarkAsRead();
    }

    protected override void OnInitialized()
    {
        DeliveryService.Subscribe(CurrentUserId, HandleIncomingMessage);
    }

    private async Task LoadMessages()
    {
        _loading = true;

        await using var scope = ScopeFactory.CreateAsyncScope();
        var chatManager = scope.ServiceProvider.GetRequiredService<IChatManager>();

        _messages = await chatManager.GetMessages(ConversationId, CurrentUserId);
        // Messages come newest-first from API, reverse for display (flex-direction: column-reverse handles visual order)

        var conversations = await chatManager.GetConversationsForUser(CurrentUserId);
        var current = conversations.FirstOrDefault(c => c.Id == ConversationId);
        _conversationTitle = current?.DisplayName ?? "Sohbet";

        _loading = false;
    }

    private async Task MarkAsRead()
    {
        await using var scope = ScopeFactory.CreateAsyncScope();
        var chatManager = scope.ServiceProvider.GetRequiredService<IChatManager>();
        await chatManager.MarkConversationAsRead(ConversationId, CurrentUserId);
    }

    private async Task HandleIncomingMessage(ChatMessageEvent evt)
    {
        if (evt.ConversationId != ConversationId)
            return;

        await InvokeAsync(async () =>
        {
            var newMessage = new ChatMessageDto(
                Id: evt.MessageId,
                SenderId: evt.SenderId,
                SenderName: evt.SenderName,
                Content: evt.Content,
                MessageType: evt.MessageType,
                CreatedAt: evt.OccurredAt,
                IsRead: true,
                ReplyToMessageId: null);

            _messages.Insert(0, newMessage);
            await MarkAsRead();
            StateHasChanged();
        });
    }

    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(_messageText))
            return;

        _sending = true;
        var content = _messageText.Trim();
        _messageText = string.Empty;

        await using var scope = ScopeFactory.CreateAsyncScope();
        var chatManager = scope.ServiceProvider.GetRequiredService<IChatManager>();

        var dto = new SendChatMessageDto(ConversationId, content);
        var sentMessage = await chatManager.SendMessage(CurrentUserId, dto);

        var messageDto = new ChatMessageDto(
            Id: sentMessage.Id,
            SenderId: CurrentUserId,
            SenderName: string.Empty,
            Content: content,
            MessageType: ChatMessageType.Text,
            CreatedAt: sentMessage.CreatedAt,
            IsRead: true,
            ReplyToMessageId: null);

        _messages.Insert(0, messageDto);

        _sending = false;
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
            await SendMessage();
    }

    private static string GetBubbleStyle(bool isMine) =>
        isMine
            ? "max-width:70%;border-radius:16px 16px 4px 16px;background-color:var(--mud-palette-primary);color:var(--mud-palette-primary-text);"
            : "max-width:70%;border-radius:16px 16px 16px 4px;background-color:var(--mud-palette-surface);";

    public void Dispose()
        => DeliveryService.Unsubscribe(CurrentUserId, HandleIncomingMessage);
}
