namespace Entegrasyon.Blazor.Features.Dashboard;

public partial class Index
{
    private DashboardMarketplaceStatus? _marketplaceStatus;

    private async Task OnSyncCompleted()
    {
        if (_marketplaceStatus is not null)
            await _marketplaceStatus.RefreshAsync();
    }
}
