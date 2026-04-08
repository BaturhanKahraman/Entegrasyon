using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;

// MVC projesinde Microsoft.AspNetCore.Http.IResult ile çakıştığı için tam nitelikli isim
using IResult = Entegrasyon.Entity.Results.IResult;

namespace Entegrasyon.MVC.Infrastructure.BranchOffices;

public sealed class ActiveBranchOfficeAccessor(
    IHttpContextAccessor httpContextAccessor,
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ILogger<ActiveBranchOfficeAccessor> logger) : IActiveBranchOfficeAccessor
{
    public const string SessionKey = "ActiveBranchOfficeId";

    public int? GetActive()
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null) return null;
        return ctx.Session.GetInt32(SessionKey);
    }

    public async Task<int> ResolveAndStoreAsync(Guid userId, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);

        // Kullanıcıyı + LastSelected + Default + junction ofislerini tek seferde getir
        var user = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.LastSelectedBranchOfficeId,
                u.RememberLastBranchOffice,
                u.DefaultBranchOfficeId
            })
            .FirstOrDefaultAsync(ct);

        if (user is null)
        {
            logger.LogWarning("ResolveAndStoreAsync: user {UserId} not found, falling back to HQ", userId);
            var hqIdFallback = await GetHeadquartersIdAsync(db, ct);
            StoreInSession(hqIdFallback);
            return hqIdFallback;
        }

        // Priority 1: RememberLastBranchOffice + LastSelected hâlâ geçerli mi?
        if (user.RememberLastBranchOffice && user.LastSelectedBranchOfficeId.HasValue)
        {
            var lastId = user.LastSelectedBranchOfficeId.Value;
            if (await IsValidForUserAsync(db, userId, lastId, ct))
            {
                StoreInSession(lastId);
                return lastId;
            }
            logger.LogInformation(
                "LastSelectedBranchOfficeId {BranchId} for user {UserId} no longer valid, falling through",
                lastId, userId);
        }

        // Priority 2: DefaultBranchOfficeId (atanmış birincil ofis)
        if (user.DefaultBranchOfficeId.HasValue)
        {
            var defId = user.DefaultBranchOfficeId.Value;
            if (await IsBranchActiveAsync(db, defId, ct))
            {
                StoreInSession(defId);
                return defId;
            }
        }

        // Priority 3: HQ final fallback
        var hqId = await GetHeadquartersIdAsync(db, ct);
        StoreInSession(hqId);
        return hqId;
    }

    public async Task<IResult> SwitchAsync(Guid userId, int branchOfficeId, bool remember, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(ct);

        // Hedef ofis aktif mi?
        if (!await IsBranchActiveAsync(db, branchOfficeId, ct))
            return new ErrorResult("Hedef şube aktif değil veya bulunamadı.");

        // Kullanıcının bu ofise erişimi var mı?
        if (!await IsValidForUserAsync(db, userId, branchOfficeId, ct))
            return new ErrorResult("Bu şubeye erişim yetkiniz bulunmuyor.");

        StoreInSession(branchOfficeId);

        if (remember)
        {
            // Kalıcı tercihi DB'ye yaz
            await db.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.LastSelectedBranchOfficeId, branchOfficeId)
                    .SetProperty(x => x.RememberLastBranchOffice, true), ct);
            logger.LogInformation(
                "User {UserId} persisted LastSelectedBranchOffice={BranchId} with remember=true",
                userId, branchOfficeId);
        }
        else
        {
            // remember=false → kullanıcı mevcut "beni hatırla" tercihini kaldırıyor mu? Hayır, geçici değişim.
            // Kalıcı alanlara dokunmuyoruz; sadece Session güncellendi.
            logger.LogInformation(
                "User {UserId} switched session ActiveBranchOffice to {BranchId} (non-persistent)",
                userId, branchOfficeId);
        }

        return new SuccessResult("Aktif şube değiştirildi.");
    }

    public async Task<bool> ValidateAndResetIfStaleAsync(Guid userId, CancellationToken ct = default)
    {
        var sessionId = GetActive();
        if (sessionId is null)
        {
            // Session boş → login middleware sonrası çalışıyor, resolve et
            await ResolveAndStoreAsync(userId, ct);
            return false;
        }

        await using var db = await contextFactory.CreateDbContextAsync(ct);

        if (await IsValidForUserAsync(db, userId, sessionId.Value, ct))
            return true;

        // Stale → HQ'ya reset
        logger.LogInformation(
            "Active branch {BranchId} for user {UserId} became stale, resetting to HQ",
            sessionId, userId);

        var hqId = await GetHeadquartersIdAsync(db, ct);
        StoreInSession(hqId);
        return false;
    }

    // ── private helpers ────────────────────────────────────────────────────

    private void StoreInSession(int branchOfficeId)
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null) return;
        ctx.Session.SetInt32(SessionKey, branchOfficeId);
    }

    private static async Task<int> GetHeadquartersIdAsync(IntegrationDbContext db, CancellationToken ct)
    {
        var hqId = await db.BranchOffices
            .Where(b => b.IsHeadquarters && !b.IsDeleted)
            .Select(b => b.Id)
            .FirstOrDefaultAsync(ct);

        if (hqId == 0)
            throw new InvalidOperationException(
                "Sistemde aktif Headquarters (IsHeadquarters=true) bir şube ofisi bulunamadı. Seed bozuk olabilir.");

        return hqId;
    }

    private static async Task<bool> IsBranchActiveAsync(IntegrationDbContext db, int branchOfficeId, CancellationToken ct)
    {
        return await db.BranchOffices
            .AnyAsync(b => b.Id == branchOfficeId && !b.IsDeleted, ct);
    }

    /// <summary>
    /// Geçerlilik kriteri:
    ///   (a) Branch exists && !IsDeleted, VE
    ///   (b) (IsHeadquarters) VEYA (junction üyeliği var) VEYA (DefaultBranchOfficeId == branchId)
    /// </summary>
    private static async Task<bool> IsValidForUserAsync(
        IntegrationDbContext db, Guid userId, int branchOfficeId, CancellationToken ct)
    {
        var info = await db.BranchOffices
            .Where(b => b.Id == branchOfficeId && !b.IsDeleted)
            .Select(b => new { b.IsHeadquarters })
            .FirstOrDefaultAsync(ct);

        if (info is null) return false;        // (a) fail
        if (info.IsHeadquarters) return true;  // (b) HQ herkese açık

        // Junction membership VEYA DefaultBranchOfficeId
        var hasAccess = await db.Users
            .AnyAsync(u => u.Id == userId && (
                u.DefaultBranchOfficeId == branchOfficeId ||
                u.UserBranchOffices.Any(ubo => ubo.BranchOfficeId == branchOfficeId)
            ), ct);

        return hasAccess;
    }
}
