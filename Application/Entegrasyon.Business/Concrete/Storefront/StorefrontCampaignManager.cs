using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontCampaignManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IStorefrontCampaignManager
{
    public async Task<IDataResult<StorefrontEmailCampaign>> CreateCampaignAsync(
        int tenantId, string subject, string htmlContent, CampaignTarget target)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        if (string.IsNullOrWhiteSpace(subject))
            return new ErrorDataResult<StorefrontEmailCampaign>(null!, "Konu alanı zorunludur.");

        if (string.IsNullOrWhiteSpace(htmlContent))
            return new ErrorDataResult<StorefrontEmailCampaign>(null!, "İçerik alanı zorunludur.");

        var campaign = new StorefrontEmailCampaign
        {
            TenantId = tenantId,
            Subject = subject,
            HtmlContent = htmlContent,
            Target = target,
            Status = CampaignStatus.Draft
        };

        dbContext.StorefrontEmailCampaigns.Add(campaign);
        await dbContext.SaveChangesAsync();

        return new SuccessDataResult<StorefrontEmailCampaign>(campaign, "Kampanya olusturuldu.");
    }

    public async Task<IDataResult<List<StorefrontEmailCampaign>>> GetCampaignsAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var campaigns = await dbContext.StorefrontEmailCampaigns
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

        return new SuccessDataResult<List<StorefrontEmailCampaign>>(campaigns);
    }

    public async Task<IResult> UpdateCampaignAsync(int id, string subject, string htmlContent, CampaignTarget target)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var campaign = await dbContext.StorefrontEmailCampaigns
            .FirstOrDefaultAsync(x => x.Id == id);

        if (campaign is null)
            return new ErrorResult("Kampanya bulunamadı.");

        if (campaign.Status != CampaignStatus.Draft)
            return new ErrorResult("Sadece taslak kampanyalar duzenlenebilir.");

        campaign.Subject = subject;
        campaign.HtmlContent = htmlContent;
        campaign.Target = target;

        dbContext.StorefrontEmailCampaigns.Update(campaign);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Kampanya guncellendi.");
    }

    public async Task<IResult> ScheduleCampaignAsync(int id, DateTimeOffset scheduleAt)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var campaign = await dbContext.StorefrontEmailCampaigns
            .FirstOrDefaultAsync(x => x.Id == id);

        if (campaign is null)
            return new ErrorResult("Kampanya bulunamadı.");

        if (campaign.Status != CampaignStatus.Draft)
            return new ErrorResult("Sadece taslak kampanyalar zamanlanabilir.");

        if (scheduleAt <= DateTimeOffset.UtcNow)
            return new ErrorResult("Zamanlama tarihi gelecekte olmalidir.");

        campaign.Status = CampaignStatus.Scheduled;
        campaign.ScheduledAt = scheduleAt;

        dbContext.StorefrontEmailCampaigns.Update(campaign);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Kampanya zamanlandi.");
    }

    public async Task<IResult> CancelCampaignAsync(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var campaign = await dbContext.StorefrontEmailCampaigns
            .FirstOrDefaultAsync(x => x.Id == id);

        if (campaign is null)
            return new ErrorResult("Kampanya bulunamadı.");

        if (campaign.Status is CampaignStatus.Sent or CampaignStatus.Sending)
            return new ErrorResult("Gonderilmis veya gonderiliyor olan kampanyalar iptal edilemez.");

        campaign.Status = CampaignStatus.Cancelled;

        dbContext.StorefrontEmailCampaigns.Update(campaign);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Kampanya iptal edildi.");
    }

    public async Task<IResult> DeleteCampaignAsync(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var campaign = await dbContext.StorefrontEmailCampaigns
            .FirstOrDefaultAsync(x => x.Id == id);

        if (campaign is null)
            return new ErrorResult("Kampanya bulunamadı.");

        if (campaign.Status == CampaignStatus.Sending)
            return new ErrorResult("Gonderiliyor olan kampanyalar silinemez.");

        campaign.IsDeleted = true;
        campaign.DeletedAt = DateTimeOffset.UtcNow;

        dbContext.StorefrontEmailCampaigns.Update(campaign);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Kampanya silindi.");
    }
}
