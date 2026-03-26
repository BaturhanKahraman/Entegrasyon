using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontCampaignManager
{
    Task<IDataResult<StorefrontEmailCampaign>> CreateCampaignAsync(int tenantId, string subject, string htmlContent, CampaignTarget target);
    Task<IDataResult<List<StorefrontEmailCampaign>>> GetCampaignsAsync(int tenantId);
    Task<IResult> UpdateCampaignAsync(int id, string subject, string htmlContent, CampaignTarget target);
    Task<IResult> ScheduleCampaignAsync(int id, DateTimeOffset scheduleAt);
    Task<IResult> CancelCampaignAsync(int id);
    Task<IResult> DeleteCampaignAsync(int id);
}
