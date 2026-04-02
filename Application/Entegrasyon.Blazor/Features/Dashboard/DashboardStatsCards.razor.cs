using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Entity.Dtos.Dashboard;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.Dashboard;

public partial class DashboardStatsCards
{
    [Inject] private IDashboardManager DashboardManager { get; set; } = null!;
    [Inject] private ITenantContext TenantContext { get; set; } = null!;

    private DashboardStatsDto _stats = new(0, 0, 0, 0m, 0, 0);
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        if (!TenantContext.IsInitialized) return;
        _stats = await DashboardManager.GetStatsAsync();
        _loading = false;
    }
}
