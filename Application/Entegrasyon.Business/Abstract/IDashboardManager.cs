using Entegrasyon.Entity.Dtos.Dashboard;

namespace Entegrasyon.Business.Abstract;

public interface IDashboardManager
{
    Task<DashboardDto> GetDashboardAsync(int lowStockThreshold = 5);
}
