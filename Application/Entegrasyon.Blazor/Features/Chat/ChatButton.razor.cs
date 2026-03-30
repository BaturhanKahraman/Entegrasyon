using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Chat;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.Blazor.Features.Chat;

public partial class ChatButton : ComponentBase, IDisposable
{
    [Inject] private IChatDeliveryService DeliveryService { get; set; } = null!;
    [Inject] private IServiceScopeFactory ScopeFactory { get; set; } = null!;
    [CascadingParameter] private Task<AuthenticationState> AuthStateTask { get; set; } = null!;

    private Guid _userId;
    private int _unreadCount;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        var authState = await AuthStateTask;
        var userIdClaim = authState.User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out _userId))
            return;

        await using var scope = ScopeFactory.CreateAsyncScope();
        var chatManager = scope.ServiceProvider.GetRequiredService<IChatManager>();
        _unreadCount = await chatManager.GetTotalUnreadCount(_userId);

        DeliveryService.Subscribe(_userId, HandleChatMessage);
        StateHasChanged();
    }

    private async Task HandleChatMessage(ChatMessageEvent evt)
    {
        await InvokeAsync(() =>
        {
            _unreadCount++;
            StateHasChanged();
        });
    }

    public void Dispose()
        => DeliveryService.Unsubscribe(_userId, HandleChatMessage);
}
