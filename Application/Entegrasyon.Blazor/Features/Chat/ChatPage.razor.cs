using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Chat;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Chat;

public partial class ChatPage : ComponentBase, IDisposable
{
    [Parameter] public long ConversationId { get; set; }

    [Inject] private IChatDeliveryService DeliveryService { get; set; } = null!;
    [Inject] private IServiceScopeFactory ScopeFactory { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [CascadingParameter] private Task<AuthenticationState> AuthStateTask { get; set; } = null!;

    private Guid _userId;
    private long _selectedConversationId;
    private ConversationList? _conversationListRef;

    protected override void OnParametersSet()
    {
        if (ConversationId > 0)
            _selectedConversationId = ConversationId;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        var authState = await AuthStateTask;
        var userIdClaim = authState.User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out _userId))
            return;

        DeliveryService.Subscribe(_userId, HandleNewMessage);
        StateHasChanged();
    }

    private async Task HandleNewMessage(ChatMessageEvent evt)
    {
        await InvokeAsync(async () =>
        {
            if (_conversationListRef is not null)
                await _conversationListRef.RefreshConversations();

            StateHasChanged();
        });
    }

    private void OnConversationSelected(long conversationId)
    {
        _selectedConversationId = conversationId;
        NavigationManager.NavigateTo($"/chat/{conversationId}", replace: true);
    }

    public void Dispose()
        => DeliveryService.Unsubscribe(_userId, HandleNewMessage);
}
