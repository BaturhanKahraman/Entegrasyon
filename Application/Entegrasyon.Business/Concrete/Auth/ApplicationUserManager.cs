using System.Security.Claims;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Entity.Dtos.Users;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.User;
using Entegrasyon.Entity.Results;
using Entegrasyon.Business.Extensions;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Requests;

namespace Entegrasyon.Business.Concrete.Auth;

public class ApplicationUserManager(
    UserMapper mapper,
    IApplicationLogManager applicationLogManager,
    Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor,
    IFluentValidator validator,
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ILogger<ApplicationUserManager> logger) : IApplicationUserManager
{
    public async Task<IResult> AddUser(AddUserDto dto, CancellationToken token = default)
    {
        await applicationLogManager.AddLog("Kullanici ekleniyor...", LogType.User, LogAction.Add, dto, token);
        var validationResult = await validator.Validate(dto);
        if (!validationResult.IsValid)
            return validationResult.ToResult();

        await using var context = await contextFactory.CreateDbContextAsync();
        var user = mapper.MapToEntity(dto);
        user.NeedsTakeNewPassword = true;
        BumpSecurityStamp(user);
        user.NormalizedUserName = user.UserName!.ToUpperInvariant();
        user.NormalizedEmail = user.Email!.ToUpperInvariant();
        user.CreatedAt = DateTimeOffset.UtcNow;

        var logicResult = LogicRunner.Run(await CheckSameUserName(context, user.NormalizedUserName));
        if (logicResult != null)
            return logicResult;
        await context.Users.AddAsync(user, token);
        await context.SaveChangesAsync(token);

        await applicationLogManager.AddLog("Kullanici eklendi.", LogType.User, LogAction.Add, token: token);
        return new SuccessResult(Messages.UserAdded);
    }

    private static async Task<IResult> CheckSameUserName(IntegrationDbContext context, string normalizedUserName)
    {
        var exists = await context.Users.AnyAsync(u => u.NormalizedUserName == normalizedUserName);
        return exists ? new ErrorResult(Messages.UserSameUsername) : new SuccessResult();
    }

    public async Task<IResult> EditUser(UserEditDto dto, CancellationToken token = default)
    {
        await applicationLogManager.AddLog("Kullanici guncelleniyor...", LogType.User, LogAction.Update, dto, token);

        // 1. Validation
        var validationResult = await validator.Validate(dto);
        if (!validationResult.IsValid)
            return validationResult.ToResult();

        // 2. Business Rules
        await using var context = await contextFactory.CreateDbContextAsync();
        var dbUser = await context.Users
            .Include(u => u.UsersRoles)
            .FirstOrDefaultAsync(u => u.Id == dto.Id, token);
        if (dbUser == null)
            return new ErrorResult(Messages.UserNotFound);

        // Kullanici adi Değiştiriliyorsa, baska kullanicida ayni isim var mi kontrol et
        var newNormalizedUserName = dto.UserName.ToUpperInvariant();
        if (dbUser.NormalizedUserName != newNormalizedUserName)
        {
            var logicResult = LogicRunner.Run(await CheckSameUserName(context, newNormalizedUserName));
            if (logicResult != null)
                return logicResult;
        }

        // 3. Execution
        dbUser.Name = dto.Name;
        dbUser.Surname = dto.Surname;
        dbUser.FullName = $"{dto.Name} {dto.Surname}";
        dbUser.Email = dto.Email;
        dbUser.NormalizedEmail = dto.Email.ToUpperInvariant();
        dbUser.UserName = dto.UserName;
        dbUser.NormalizedUserName = newNormalizedUserName;
        dbUser.IsActive = dto.IsActive;
        dbUser.DefaultBranchOfficeId = dto.BranchOfficeId;

        // Rol atamasi: mevcut rolleri temizle, yeni rolleri ekle
        dbUser.UsersRoles.Clear();
        foreach (var roleId in dto.RoleIds)
        {
            dbUser.UsersRoles.Add(new UsersRoles
            {
                ApplicationUserId = dbUser.Id,
                RoleId = roleId
            });
        }

        try
        {
            await context.SaveChangesAsync(token);
        }
        catch (DbUpdateConcurrencyException e)
        {
            logger.LogError(e, "Concurrency Exception");
            return new ErrorResult("Bu kayit guncellenmis ve sizdeki versiyonu eski olabilir. Lutfen tekrar deneyin");
        }

        await applicationLogManager.AddLog("Kullanici guncellendi.", LogType.User, LogAction.Update, dto, token);
        return new SuccessResult(Messages.UserUpdated);
    }

    public Task<IDataResult<Pageable<UserDetailListDto>>> GetPaginatedUserDetails(int pageIndex = 0, int itemCount = 50)
        => GetPaginatedUserDetails(new UserPaginatedRequest() { PageIndex = pageIndex, PageSize = itemCount });

    public async Task<IDataResult<Pageable<UserDetailListDto>>> GetPaginatedUserDetails(UserPaginatedRequest request)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var result = await context.Users.AsNoTracking().ApplyGlobalSearch(request.SearchTerm,
            nameof(ApplicationUser.FullName),
            nameof(ApplicationUser.NormalizedUserName),
            nameof(ApplicationUser.NormalizedEmail))
            .Select(u=>
                new UserDetailListDto(u.Id,
                    u.Name!,
                    u.Surname!,
                    u.UserName!,
                    u.IsActive,
                    u.IsTwoFactorAuthActive,
                    u.NeedsTakeNewPassword,
                    u.CreatedAt,
                    u.DefaultBranchOffice != null ? u.DefaultBranchOffice.Name! : "",
                    string.Join(", ", u.Roles.Select(r => r.Name))
                    ))
            .ToPageableAsync(request);

        return new SuccessDataResult<Pageable<UserDetailListDto>>(result);
    }

    public async Task<IDataResult<UserDetailDto>> GetUserDetails(Guid id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var result = await context.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserDetailDto(
                u.Id, u.Name!, u.Surname!, u.UserName!,
                u.IsActive, u.IsTwoFactorAuthActive, u.NeedsTakeNewPassword, u.CreatedAt,
                u.DefaultBranchOffice != null ? u.DefaultBranchOffice.Name! : "",
                u.Roles.Select(r => r.Name).FirstOrDefault() ?? ""))
            .FirstOrDefaultAsync();
        return new SuccessDataResult<UserDetailDto>(result!);
    }

    public async Task<IDataResult<UserDetailDto>> GetUserDetails(string id)
    {
        var convertable = Guid.TryParse(id, out var guidId);
        if (!convertable)
            return new ErrorDataResult<UserDetailDto>(null!, Messages.ProcessFailed);
        return await GetUserDetails(guidId);
    }

    public async Task<IDataResult<UserEditDetailDto>> GetUserEditDetail(Guid id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var result = await context.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserEditDetailDto(
                u.Id, u.Name!, u.Surname!, u.UserName!, u.Email!,
                u.IsActive, u.IsTwoFactorAuthActive, u.NeedsTakeNewPassword,
                u.DefaultBranchOfficeId,
                u.DefaultBranchOffice != null ? u.DefaultBranchOffice.Name! : "",
                u.Roles.Select(r => r.Id).ToList(),
                u.Roles.Select(r => r.Name).FirstOrDefault() ?? "",
                u.CreatedAt))
            .FirstOrDefaultAsync();
        if (result is null)
            return new ErrorDataResult<UserEditDetailDto>(null!, Messages.UserNotFound);
        return new SuccessDataResult<UserEditDetailDto>(result);
    }
    public string GetActiveUserId() =>
       GetActiveUserGuidId().ToString();

    public Guid GetActiveUserGuidId() => Guid.Parse(httpContextAccessor.HttpContext!.User
        .FindFirst(x => x.Type == ClaimTypes.NameIdentifier)
        ?.Value!);


    public async ValueTask<ApplicationUser> GetUserById(Guid id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return (await context.Users.FindAsync(id))!;
    }

    public async Task<IResult> SetPassive(Guid userId, CancellationToken token = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var user = await context.Users.FindAsync(userId);
        if (user is null)
        {
            return new ErrorResult(Messages.UserNotFound);
        }
        user.IsActive = false;
        // Pasifleştirme → aktif cookie oturumunu düşür (auto-logout).
        BumpSecurityStamp(user);
        // Context global no-tracking (TenantDbContextFactory) → mutasyon persist olması için Update şart.
        context.Update(user);
        try
        {
            await context.SaveChangesAsync(token);
        }
        catch (DbUpdateConcurrencyException e)
        {
            logger.LogError(e, "Concurrency hatasi");
            return new ErrorResult("Bu kayit guncellenmis ve sizdeki versiyonu eski olabilir. Lutfen tekrar deneyin");
        }
        return new SuccessResult(Messages.ProcessSuccess);
    }

    public async Task<IResult> ToggleActive(Guid userId, CancellationToken token = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var user = await context.Users.FindAsync(userId);
        if (user is null)
            return new ErrorResult(Messages.UserNotFound);
        user.IsActive = !user.IsActive;
        // Yalnızca pasifleştirirken oturumu düşür; reaktivasyonda gerek yok.
        if (!user.IsActive)
            BumpSecurityStamp(user);
        // Context global no-tracking → mutasyon persist olması için Update şart.
        context.Update(user);
        try
        {
            await context.SaveChangesAsync(token);
        }
        catch (DbUpdateConcurrencyException e)
        {
            logger.LogError(e, "Concurrency hatasi");
            return new ErrorResult("Bu kayit guncellenmis ve sizdeki versiyonu eski olabilir. Lutfen tekrar deneyin");
        }
        var statusText = user.IsActive ? "aktif" : "pasif";
        await applicationLogManager.AddLog($"Kullanici {statusText} yapildi.", LogType.User, LogAction.Update, token: token);
        return new SuccessResult($"Kullanici basariyla {statusText} yapildi.");
    }

    public async Task<IResult> SoftDelete(Guid userId, CancellationToken token = default)
    {
        await applicationLogManager.AddLog("Kullanici siliniyor...", LogType.User, LogAction.Delete, token: token);
        await using var context = await contextFactory.CreateDbContextAsync();
        var user = await context.Users.FindAsync([userId], cancellationToken: token);
        if (user is null)
            return new ErrorResult(Messages.UserNotFound);

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        // Silinen kullanıcının aktif cookie oturumunu düşür (auto-logout).
        BumpSecurityStamp(user);
        // Context global no-tracking → mutasyon persist olması için Update şart.
        context.Update(user);
        await context.SaveChangesAsync(token);

        logger.LogInformation("User soft-deleted {UserId}", userId);
        await applicationLogManager.AddLog("Kullanici silindi.", LogType.User, LogAction.Delete, token: token);
        return new SuccessResult(Messages.UserDeletedSuccessfuly);
    }

    public async Task<IDataResult<string>> AdminResetPassword(Guid userId, CancellationToken token = default)
    {
        await applicationLogManager.AddLog("Yonetici sifre sifirlama islemi basliyor...", LogType.User, LogAction.Update, token: token);

        // 2. Business Rules — kullanici var mi kontrolu (validasyon gerektiren input yok)
        await using var context = await contextFactory.CreateDbContextAsync();
        var user = await context.Users.FindAsync([userId], cancellationToken: token);
        if (user is null)
            return new ErrorDataResult<string>(null!, Messages.UserNotFound);

        // 3. Execution — geçici şifre ata, ilk girişte değiştirmeye zorla, oturumu düşür.
        var temporaryPassword = GenerateTemporaryPassword();
        user.NeedsTakeNewPassword = true;
        user.TemporaryPassword = temporaryPassword;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        BumpSecurityStamp(user);
        // Context global no-tracking → mutasyon persist olması için Update şart.
        context.Update(user);

        try
        {
            await context.SaveChangesAsync(token);
        }
        catch (DbUpdateConcurrencyException e)
        {
            logger.LogError(e, "Sifre sifirlama sirasinda concurrency hatasi {UserId}", userId);
            return new ErrorDataResult<string>(null!, "Bu kayit guncellenmis olabilir. Lutfen tekrar deneyin.");
        }

        logger.LogInformation("Admin reset password for user {UserId}", userId);
        await applicationLogManager.AddLog("Yonetici kullanici sifresini sifirladi.", LogType.User, LogAction.Update, token: token);
        return new SuccessDataResult<string>(temporaryPassword, Messages.TemporaryPasswordAssigned);
    }

    /// <summary>
    /// Güvenlik damgasını yeniler. Bu kullanıcının cookie'sindeki damga artık DB ile eşleşmez →
    /// bir sonraki istekte oturum düşer (auto-logout). MaxLength(32) ile uyumlu: 32 hex karakter.
    /// </summary>
    private static void BumpSecurityStamp(ApplicationUser user)
        => user.SecurityStamp = Guid.NewGuid().ToString("N");

    /// <summary>Yönetici sıfırlamasında kullanıcıya verilecek geçici şifre. TemporaryPassword MaxLength(15) ile uyumlu.</summary>
    private static string GenerateTemporaryPassword()
        => Guid.NewGuid().ToString("N")[..12];

    public async Task<IResult> UpdateOwnProfile(Guid userId, UpdateProfileDto dto, CancellationToken token = default)
    {
        // 1. Validation
        var validationResult = await validator.Validate(dto);
        if (!validationResult.IsValid)
            return validationResult.ToResult();

        // 2. Business Rules — kullanici var mi kontrolu
        await using var context = await contextFactory.CreateDbContextAsync();
        var user = await context.Users.FindAsync([userId], cancellationToken: token);
        if (user is null)
            return new ErrorResult(Messages.UserNotFound);

        // 3. Execution
        user.Name = dto.Name;
        user.Surname = dto.Surname;
        user.FullName = $"{dto.Name} {dto.Surname}";
        user.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await context.SaveChangesAsync(token);
        }
        catch (DbUpdateConcurrencyException e)
        {
            logger.LogError(e, "Profil guncelleme sirasinda concurrency hatasi");
            return new ErrorResult("Bu kayit guncellenmis ve sizdeki versiyonu eski olabilir. Lutfen tekrar deneyin");
        }

        await applicationLogManager.AddLog("Kullanici profil bilgilerini guncelledi.", LogType.User, LogAction.Update, token: token);
        return new SuccessResult(Messages.ProfileUpdated);
    }

    /// <summary>Online sayılma eşiği — son bu süre içinde istek atmış kullanıcı "online".</summary>
    private static readonly TimeSpan OnlineThreshold = TimeSpan.FromMinutes(5);

    public async Task<IDataResult<UserActivitySummaryDto>> GetUserActivitySummary(
        Guid id, CancellationToken token = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(token);

        // Salt-okuma yolu: AddLog'u her çağrıda yazma (log-spam); sadece hata durumunda.
        var user = await context.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new { u.FullName, u.UserName, u.IsActive, u.LastSeenAt })
            .FirstOrDefaultAsync(token);

        if (user is null)
        {
            logger.LogWarning("Kullanici aktivite ozeti istendi ama kullanici bulunamadi {UserId}", id);
            await applicationLogManager.AddLog(
                "Kullanici aktivite ozeti istendi ama kullanici bulunamadi.",
                LogType.User, LogAction.List, token: token);
            return new ErrorDataResult<UserActivitySummaryDto>(null!, Messages.UserNotFound);
        }

        // Ürün ekleme/güncelleme/silme sayıları kullanıcı-facing ApplicationLog'dan (LogType.Product)
        // türetilir — tek sorgu, indexli LogAction üzerinden grupla.
        var productCounts = await context.Logs.AsNoTracking()
            .Where(l => l.ApplicationUserId == id && l.LogType == LogType.Product
                && (l.LogAction == LogAction.Add
                    || l.LogAction == LogAction.Update
                    || l.LogAction == LogAction.Delete))
            .GroupBy(l => l.LogAction)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .ToListAsync(token);

        var added = productCounts.FirstOrDefault(c => c.Action == LogAction.Add)?.Count ?? 0;
        var updated = productCounts.FirstOrDefault(c => c.Action == LogAction.Update)?.Count ?? 0;
        var deleted = productCounts.FirstOrDefault(c => c.Action == LogAction.Delete)?.Count ?? 0;

        var salesCount = await context.Sales.AsNoTracking()
            .CountAsync(s => s.SalePersonId == id, token);

        var isOnline = user.LastSeenAt.HasValue
            && DateTimeOffset.UtcNow - user.LastSeenAt.Value <= OnlineThreshold;

        logger.LogInformation("Kullanici aktivite ozeti getirildi {UserId}", id);

        var dto = new UserActivitySummaryDto(
            id,
            user.FullName ?? $"{user.UserName}",
            user.UserName ?? "",
            user.IsActive,
            user.LastSeenAt,
            isOnline,
            added,
            updated,
            deleted,
            salesCount);

        return new SuccessDataResult<UserActivitySummaryDto>(dto);
    }
}
