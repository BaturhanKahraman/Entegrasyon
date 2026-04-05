using Entegrasyon.Entity.Dtos.Log;

namespace Entegrasyon.MVC.Features.IntegrationHealth.ViewModels;

public class IntegrationHealthVm
{
    public int RecentSyncCount { get; set; }
    public int RecentErrorCount { get; set; }
    public DateTimeOffset? LastSyncTime { get; set; }
    public List<ApplicationLogDetailDto> RecentErrors { get; set; } = [];
}
