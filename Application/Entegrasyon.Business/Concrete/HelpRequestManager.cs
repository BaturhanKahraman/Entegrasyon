using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Help;
using Entegrasyon.Entity.Help;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

public sealed class HelpRequestManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IFluentValidator validator,
    HelpRequestMapper mapper,
    IApplicationLogManager applicationLogManager,
    ITenantContext tenantContext,
    ILogger<HelpRequestManager> logger) : IHelpRequestManager
{
    private int TenantId => tenantContext.IsInitialized ? tenantContext.TenantId : 1;

    public async Task<IResult> CreateAsync(CreateHelpRequestDto dto, Guid userId)
    {
        await applicationLogManager.AddLog("Yardım talebi gönderiliyor.", LogType.Help, LogAction.Add);

        // 1. Validation
        await validator.ValidateAndThrowAsync(dto);

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // 2. Business Rules
        var rule = LogicRunner.Run(CheckUser(userId));
        if (rule != null)
        {
            await applicationLogManager.AddLog($"Yardım talebi gönderilemedi. {rule.Message}", LogType.Help, LogAction.Add);
            return new ErrorResult(rule.Message!);
        }

        // 3. Execution
        var entity = mapper.MapToEntity(dto);
        entity.TenantId = TenantId;
        entity.UserId = userId;
        entity.Status = HelpRequestStatus.Open;

        dbContext.Set<HelpRequest>().Add(entity);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog($"Yardım talebi alındı: {entity.Subject}", LogType.Help, LogAction.Add);
        logger.LogInformation("HelpRequest created {Id} by user {UserId} (tenant {TenantId})", entity.Id, userId, entity.TenantId);

        return new SuccessResult("Yardım talebiniz alındı. En kısa sürede dönüş yapacağız.");
    }

    public async Task<List<HelpRequestListDto>> GetAllAsync()
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var tenantId = TenantId;

        return await dbContext.Set<HelpRequest>()
            .AsNoTracking()
            .Where(h => h.TenantId == tenantId)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new HelpRequestListDto(
                h.Id,
                h.Subject,
                h.Category,
                h.Status,
                dbContext.Users
                    .Where(u => u.Id == h.UserId)
                    .Select(u => u.FullName ?? (u.Name + " " + u.Surname).Trim())
                    .FirstOrDefault(),
                h.CreatedAt))
            .ToListAsync();
    }

    public async Task<IDataResult<HelpRequestDetailDto>> GetDetailAsync(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var tenantId = TenantId;

        var row = await dbContext.Set<HelpRequest>()
            .AsNoTracking()
            .Where(h => h.TenantId == tenantId && h.Id == id)
            .Select(h => new
            {
                h.Id,
                h.Subject,
                h.Message,
                h.Category,
                h.Status,
                h.UserId,
                h.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (row is null)
        {
            logger.LogWarning("HelpRequest detail not found {Id} (tenant {TenantId})", id, tenantId);
            return new ErrorDataResult<HelpRequestDetailDto>(null!, "Yardım talebi bulunamadı.");
        }

        // Tek PK-lookup ile gönderenin ad + e-postasını birlikte al (çift subquery yerine).
        var sender = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == row.UserId)
            .Select(u => new { Name = u.FullName ?? (u.Name + " " + u.Surname).Trim(), u.Email })
            .FirstOrDefaultAsync();

        var detail = new HelpRequestDetailDto(
            row.Id, row.Subject, row.Message, row.Category, row.Status,
            sender?.Name, sender?.Email, row.CreatedAt);

        return new SuccessDataResult<HelpRequestDetailDto>(detail);
    }

    public async Task<IResult> MarkResolvedAsync(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var tenantId = TenantId;

        // Global no-tracking varsayilani altinda guncellenecek entity izlenmeli.
        var entity = await dbContext.Set<HelpRequest>()
            .AsTracking()
            .FirstOrDefaultAsync(h => h.TenantId == tenantId && h.Id == id);

        if (entity is null)
        {
            await applicationLogManager.AddLog($"Yardım talebi çözüldü işaretlenemedi: #{id} bulunamadı.", LogType.Help, LogAction.Update);
            return new ErrorResult("Yardım talebi bulunamadı.");
        }

        if (entity.Status == HelpRequestStatus.Resolved)
            return new SuccessResult("Talep zaten çözüldü olarak işaretli.");

        entity.Status = HelpRequestStatus.Resolved;
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog($"Yardım talebi çözüldü olarak işaretlendi: {entity.Subject}", LogType.Help, LogAction.Update);
        logger.LogInformation("HelpRequest resolved {Id} (tenant {TenantId})", id, tenantId);

        return new SuccessResult("Talep çözüldü olarak işaretlendi.");
    }

    private static IResult CheckUser(Guid userId)
        => userId == Guid.Empty
            ? new ErrorResult("Geçersiz kullanıcı.")
            : new SuccessResult();
}
