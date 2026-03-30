using Microsoft.AspNetCore.SignalR;

namespace Entegrasyon.Business.Notifications.SignalR;

public class ChatHub : Hub
{
    public async Task JoinConversation(long conversationId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"chat-{conversationId}");

    public async Task LeaveConversation(long conversationId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat-{conversationId}");

    public async Task SendTyping(long conversationId, bool isTyping)
    {
        var userId = Context.UserIdentifier;
        await Clients.OthersInGroup($"chat-{conversationId}")
            .SendAsync("UserTyping", conversationId, userId, isTyping);
    }
}
