using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Stock;

public class NegativeStockIncidentManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    ILogger<NegativeStockIncidentManager> logger) : INegativeStockIncidentManager
{
    public async Task<IDataResult<NegativeStockIncident>> CreateAsync(CreateIncidentDto dto)
    {
        if (dto.TenantId <= 0)
            return new ErrorDataResult<NegativeStockIncident>(null!, "Geçersiz tenant.");
        if (string.IsNullOrWhiteSpace(dto.TriggeringSource))
            return new ErrorDataResult<NegativeStockIncident>(null!, "Tetikleyen kaynak belirtilmeli.");
        if (dto.Quantity <= 0)
            return new ErrorDataResult<NegativeStockIncident>(null!, "Miktar pozitif olmalı.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var incident = new NegativeStockIncident
        {
            TenantId = dto.TenantId,
            BranchOfficeId = dto.BranchOfficeId,
            ProductVariantId = dto.ProductVariantId,
            Quantity = dto.Quantity,
            StockBefore = dto.StockBefore,
            StockAfter = dto.StockAfter,
            TriggeringSource = dto.TriggeringSource,
            TriggeringReferenceId = dto.TriggeringReferenceId,
            TriggeringSaleId = dto.TriggeringSaleId,
            DetectedAt = DateTimeOffset.UtcNow
        };

        dbContext.NegativeStockIncidents.Add(incident);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"Negatif stok tespit edildi: variant={dto.ProductVariantId} qty={dto.Quantity} src={dto.TriggeringSource}",
            LogType.StockSync,
            LogAction.Add);
        logger.LogWarning(
            "NegativeStockIncident created: Id={Id} Tenant={TenantId} Variant={Variant} Qty={Qty} Source={Source}",
            incident.Id, dto.TenantId, dto.ProductVariantId, dto.Quantity, dto.TriggeringSource);

        return new SuccessDataResult<NegativeStockIncident>(incident, "Olay kaydedildi.");
    }

    public async Task<List<NegativeStockIncident>> GetActiveAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return await dbContext.NegativeStockIncidents
            .AsNoTracking()
            .Include(i => i.ProductVariant)
            .Where(i => i.TenantId == tenantId && i.Resolution == null && !i.IsDeleted)
            .OrderByDescending(i => i.DetectedAt)
            .ToListAsync();
    }

    public async Task<IDataResult<NegativeStockIncident>> GetByIdAsync(int id, int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var incident = await dbContext.NegativeStockIncidents
            .AsNoTracking()
            .Include(i => i.ProductVariant)
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId && !i.IsDeleted);

        return incident is null
            ? new ErrorDataResult<NegativeStockIncident>(null!, "Olay bulunamadı.")
            : new SuccessDataResult<NegativeStockIncident>(incident);
    }

    public async Task<IResult> ResolveAsync(int id, int tenantId, ResolveIncidentDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var incident = await dbContext.NegativeStockIncidents
            .AsTracking()
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId && !i.IsDeleted);

        if (incident is null)
            return new ErrorResult("Olay bulunamadı.");
        if (incident.Resolution is not null)
            return new ErrorResult("Olay zaten çözümlendi.");

        incident.Resolution = dto.Resolution;
        incident.ResolvedByUserId = dto.ResolvedByUserId;
        incident.ResolvedAt = DateTimeOffset.UtcNow;
        incident.Notes = dto.Notes;
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"Negatif stok çözümlendi: incident={id} resolution={dto.Resolution}",
            LogType.StockSync,
            LogAction.Update);
        logger.LogInformation(
            "Incident resolved: Id={Id} Resolution={Resolution} User={User}",
            id, dto.Resolution, dto.ResolvedByUserId);

        return new SuccessResult("Olay çözümlendi.");
    }

    public async Task<int> GetActiveCountAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return await dbContext.NegativeStockIncidents
            .AsNoTracking()
            .CountAsync(i => i.TenantId == tenantId && i.Resolution == null && !i.IsDeleted);
    }
}
