using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Chat;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Chat;

public partial class ConversationList : ComponentBase
{
    [Parameter] public EventCallback<long> OnConversationSelected { get; set; }
    [Parameter] public long SelectedConversationId { get; set; }

    [Inject] private IServiceScopeFactory ScopeFactory { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [CascadingParameter] private Task<AuthenticationState> AuthStateTask { get; set; } = null!;

    private Guid _userId;
    private List<ChatConversationListItemDto> _conversations = [];
    private ChatConversationListItemDto? _selectedItem;
    private string _searchText = string.Empty;
    private bool _loading = true;

    private IEnumerable<ChatConversationListItemDto> FilteredConversations =>
        string.IsNullOrWhiteSpace(_searchText)
            ? _conversations
            : _conversations.Where(c =>
                c.DisplayName.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        var authState = await AuthStateTask;
        var userIdClaim = authState.User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out _userId))
            return;

        await LoadConversations();
        StateHasChanged();
    }

    public async Task RefreshConversations()
    {
        await LoadConversations();
        StateHasChanged();
    }

    private async Task LoadConversations()
    {
        _loading = true;

        await using var scope = ScopeFactory.CreateAsyncScope();
        var chatManager = scope.ServiceProvider.GetRequiredService<IChatManager>();
        _conversations = await chatManager.GetConversationsForUser(_userId);

        _loading = false;
    }

    private async Task SelectConversation(long conversationId)
    {
        SelectedConversationId = conversationId;
        await OnConversationSelected.InvokeAsync(conversationId);
    }

    private async Task OpenNewChatDialog()
    {
        var dialog = await DialogService.ShowAsync<NewChatDialog>("Yeni Sohbet",
            new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                CloseOnEscapeKey = true
            });

        var result = await dialog.Result;
        if (result is { Canceled: false, Data: long conversationId })
        {
            await LoadConversations();
            await SelectConversation(conversationId);
        }
    }

    private string GetItemClass(long conversationId) =>
        conversationId == SelectedConversationId ? "mud-primary-text" : "";

    private static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1].ToUpperInvariant(),
            _ => $"{parts[0][..1]}{parts[^1][..1]}".ToUpperInvariant()
        };
    }

    private static string FormatTime(DateTimeOffset time)
    {
        var now = DateTimeOffset.UtcNow;
        if (time.Date == now.Date)
            return time.ToLocalTime().ToString("HH:mm");
        if (time.Date == now.Date.AddDays(-1))
            return "Dun";
        return time.ToLocalTime().ToString("dd.MM");
    }
}
