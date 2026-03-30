using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.User;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Chat;

public partial class NewChatDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [CascadingParameter] private Task<AuthenticationState> AuthStateTask { get; set; } = null!;

    [Inject] private IServiceScopeFactory ScopeFactory { get; set; } = null!;

    private ApplicationUser? _selectedUser;
    private List<ApplicationUser> _availableUsers = [];
    private bool _processing;
    private Guid _userId;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        var authState = await AuthStateTask;
        var userIdClaim = authState.User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userIdClaim is not null && Guid.TryParse(userIdClaim, out _userId))
        {
            await using var scope = ScopeFactory.CreateAsyncScope();
            var chatManager = scope.ServiceProvider.GetRequiredService<IChatManager>();
            _availableUsers = await chatManager.GetAvailableChatUsers(_userId);
            StateHasChanged();
        }
    }

    private Task<IEnumerable<ApplicationUser>> SearchUsers(string? value, CancellationToken ct)
    {
        var result = string.IsNullOrWhiteSpace(value)
            ? _availableUsers
            : _availableUsers.Where(u =>
                u.FullName?.Contains(value, StringComparison.OrdinalIgnoreCase) == true);

        return Task.FromResult(result.AsEnumerable());
    }

    private async Task StartConversation()
    {
        if (_selectedUser is null) return;

        _processing = true;

        await using var scope = ScopeFactory.CreateAsyncScope();
        var chatManager = scope.ServiceProvider.GetRequiredService<IChatManager>();
        var conversation = await chatManager.GetOrCreateDirectConversation(_userId, _selectedUser.Id);

        _processing = false;
        MudDialog.Close(DialogResult.Ok(conversation.Id));
    }

    private void Cancel() => MudDialog.Cancel();

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
}
