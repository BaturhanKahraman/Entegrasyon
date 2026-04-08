using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Requests;
using Entegrasyon.Entity.POS;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Concrete;

public class BranchOfficeManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    BranchOfficeMapper mapper,
    IValidator<BranchOfficeAddDto> addValidator,
    IValidator<BranchOfficeEditDto> editValidator)
    : IBranchOfficeManager
{
    public async Task<IDataResult<List<BranchOffice>>> GetBranchList(CancellationToken token=default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return new SuccessDataResult<List<BranchOffice>>(await dbContext.BranchOffices.AsNoTracking().ToListAsync(token));
    }

    public async Task<IDataResult<BranchOffice>> AddBranch(BranchOfficeAddDto officeDto)
    {
        // 1. Validation
        var validationResult = await addValidator.ValidateAsync(officeDto);
        if (!validationResult.IsValid)
            return new ErrorDataResult<BranchOffice>(null!,
                string.Join(" ", validationResult.Errors.Select(e => e.ErrorMessage)));

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // Normalized name için Türkçe-safe dönüşüm
        var normalized = BranchNameNormalizer.Normalize(officeDto.Name);

        // 2. Business rules
        var rule = LogicRunner.Run(
            await CheckIfNormalizedNameExistsAsync(dbContext, normalized, excludedId: 0));
        if (rule != null)
            return new ErrorDataResult<BranchOffice>(null!, rule.Message!);

        // 3. Execution
        await applicationLogManager.AddLog("Şube ekleme işlemi yapılmakta.", LogType.Branch, LogAction.Add);

        var office = mapper.MapToEntity(officeDto);
        office.NormalizedName = normalized;
        office.Address = officeDto.Address;

        await dbContext.BranchOffices.AddAsync(office);
        await dbContext.SaveChangesAsync();

        // Kullanıcı ataması: explicit verildiyse o kullanıcılar, verilmediyse HQ'ya atama yapılmaz
        // (yeni şubeye kimse atanmadan kalır, kullanıcılar sonradan atanabilir)
        if (officeDto.AssignedUserIds is { Count: > 0 })
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var userId in officeDto.AssignedUserIds.Distinct())
            {
                dbContext.UserBranchOffices.Add(new UserBranchOffice
                {
                    UserId = userId,
                    BranchOfficeId = office.Id,
                    AssignedAt = now
                });

                // Kullanıcının DefaultBranchOfficeId'si hâlâ null ise bu yeni şubeyi primary yap
                var user = await dbContext.Users.AsTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId);
                if (user is not null && user.DefaultBranchOfficeId is null)
                    user.DefaultBranchOfficeId = office.Id;
            }
            await dbContext.SaveChangesAsync();
        }

        await applicationLogManager.AddLog(
            $"Şube başarıyla eklendi: {office.Name}", LogType.Branch, LogAction.Add);

        return new SuccessDataResult<BranchOffice>(office, Messages.BranchAdded);
    }

    public async Task<IDataResult<BranchDetailDto>> GetBranchDetailById(int branchId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var data = await dbContext.BranchOffices.Select(b =>
            new BranchDetailDto(b.Id, b.Name!, b.Users.Count(), b.CreatedAt))
            .FirstOrDefaultAsync(b=>b.Id==branchId);
        return new SuccessDataResult<BranchDetailDto>(data!);
    }

    public async Task<IDataResult<BranchOffice>> Update(BranchOfficeEditDto dto)
    {
        // 1. Validation
        var validationResult = await editValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            return new ErrorDataResult<BranchOffice>(null!,
                string.Join(" ", validationResult.Errors.Select(e => e.ErrorMessage)));

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var normalized = BranchNameNormalizer.Normalize(dto.Name);

        // 2. Business rules
        var rule = LogicRunner.Run(
            await CheckIfNormalizedNameExistsAsync(dbContext, normalized, excludedId: dto.Id));
        if (rule != null)
            return new ErrorDataResult<BranchOffice>(null!, rule.Message!);

        // 3. Execution
        await applicationLogManager.AddLog("Şube düzenleme işlemi yapılmakta.", LogType.Branch, LogAction.Update, dto);

        var dbOffice = await dbContext.BranchOffices.AsTracking()
            .FirstOrDefaultAsync(x => x.Id == dto.Id);
        if (dbOffice is null)
            return new ErrorDataResult<BranchOffice>(null!, "Şube bulunamadı.");

        dbOffice.Name = dto.Name;
        dbOffice.NormalizedName = normalized;
        dbOffice.Address = dto.Address;
        // IsHeadquarters DTO'da yok — DB'de immutable, asla yazılmaz

        // Junction reconciliation: eklenen/kaldırılan kullanıcıları hesapla
        if (dto.AssignedUserIds is not null)
        {
            var existing = await dbContext.UserBranchOffices
                .Where(j => j.BranchOfficeId == dto.Id)
                .Select(j => j.UserId)
                .ToListAsync();

            var desired = dto.AssignedUserIds.Distinct().ToHashSet();
            var existingSet = existing.ToHashSet();

            var toAdd = desired.Except(existingSet).ToList();
            var toRemove = existingSet.Except(desired).ToList();

            var now = DateTimeOffset.UtcNow;
            foreach (var userId in toAdd)
            {
                dbContext.UserBranchOffices.Add(new UserBranchOffice
                {
                    UserId = userId,
                    BranchOfficeId = dto.Id,
                    AssignedAt = now
                });
            }

            if (toRemove.Count > 0)
            {
                await dbContext.UserBranchOffices
                    .Where(j => j.BranchOfficeId == dto.Id && toRemove.Contains(j.UserId))
                    .ExecuteDeleteAsync();
            }
        }

        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog(
            $"Şube düzenleme işlemi başarıyla tamamlandı: {dbOffice.Name}",
            LogType.Branch, LogAction.Update);

        return new SuccessDataResult<BranchOffice>(dbOffice);
    }

    public async Task<IDataResult<List<BranchOffice>>> GetBranchesForUserAsync(Guid userId, CancellationToken token = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(token);
        var branches = await dbContext.UserBranchOffices
            .Where(j => j.UserId == userId)
            .Join(dbContext.BranchOffices.Where(b => !b.IsDeleted),
                  j => j.BranchOfficeId,
                  b => b.Id,
                  (j, b) => b)
            .OrderBy(b => b.Name)
            .ToListAsync(token);
        return new SuccessDataResult<List<BranchOffice>>(branches);
    }
    public async Task<IResult> Delete(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        await applicationLogManager.AddLog("Ofis silme işlemi yapılmakta.", LogType.Branch, LogAction.Delete);

        if (id <= 0)
            return new ErrorResult("Ofis id si alırken sıkıntı yaşanmıştır.");

        // Kontrol 1: Son aktif depo mu?
        var activeBranchCount = await dbContext.BranchOffices.CountAsync(b => !b.IsDeleted);
        if (activeBranchCount <= 1)
            return new ErrorResult("En az 1 aktif depo olmalidir.");

        // Kontrol 2: Aktif POS oturumu var mi?
        var hasPosSession = await dbContext.POSSessions
            .AnyAsync(s => s.BranchOfficeId == id && s.Status == POSSessionStatus.Open);
        if (hasPosSession)
            return new ErrorResult("Bu depoda acik POS oturumu var. Lutfen once oturumu kapatin.");

        // Kontrol 3: IsDefaultMarketPlaceStock mi?
        var branch = await dbContext.BranchOffices.AsTracking().FirstOrDefaultAsync(b => b.Id == id);
        if (branch is null)
            return new ErrorResult("Depo bulunamadı.");

        if (branch.IsDefaultMarketPlaceStock)
            return new ErrorResult("Bu depo varsayilan marketplace deposudur. Once baska bir depoyu varsayilan yapin.");

        // Soft delete
        branch.IsDeleted = true;
        branch.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog("Ofis silme işlemi başarıyla tamamlandı.", LogType.Branch, LogAction.Delete);
        return new SuccessResult("Depo başarıyla silindi.");
    }

    private async Task<IResult> CheckIfTheSameNameExists(IntegrationDbContext dbContext, string name,int id)
    {
        bool exits = await dbContext.BranchOffices.AnyAsync(b => string.Equals(name, b.Name) && b.Id != id);
        return exits ? new ErrorResult(Messages.OfficeNameAlreadyExists) : new SuccessResult();
    }

    /// <summary>
    /// Türkçe-safe normalized name çakışması kontrolü. Sadece aktif (non-deleted) ofisler arasında.
    /// Soft-deleted ofis ismi yeniden kullanılabilir (filtered unique index sayesinde).
    /// </summary>
    private static async Task<IResult> CheckIfNormalizedNameExistsAsync(
        IntegrationDbContext dbContext, string normalizedName, int excludedId)
    {
        var exists = await dbContext.BranchOffices
            .AnyAsync(b => b.NormalizedName == normalizedName && b.Id != excludedId);
        return exists ? new ErrorResult(Messages.OfficeNameAlreadyExists) : new SuccessResult();
    }

    public async Task<bool> CheckIfOfficesExits(IEnumerable<int> officeIds)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var officeIdArray = officeIds.ToArray();
        var existingCount = await dbContext.BranchOffices.CountAsync(o => officeIdArray.Contains(o.Id));
        return existingCount == officeIdArray.Length;
    }

    public async Task<IDataResult<Pageable<BranchListDetailDto>>> GetPageableBranchOffices(int pageIndex = 0, int pageSize = 50)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        int total = await dbContext.BranchOffices.CountAsync();
        var items = await dbContext.BranchOffices
            .OrderByDescending(b => b.CreatedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(b => new BranchListDetailDto(b.CreatedAt, b.Id, b.Name!, b.Users.Count()))
            .ToListAsync();
        return new SuccessDataResult<Pageable<BranchListDetailDto>>(new Pageable<BranchListDetailDto>(items, pageIndex, pageSize, total));
    }

    public async Task<IDataResult<Pageable<BranchListDetailDto>>> GetPageableBranchOffices(BranchPaginatedRequest request)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        int total = await dbContext.BranchOffices.CountAsync();
        var items = await dbContext.BranchOffices
            .OrderByDescending(b => b.CreatedAt)
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(b => new BranchListDetailDto(b.CreatedAt, b.Id, b.Name!, b.Users.Count()))
            .ToListAsync();
        return new SuccessDataResult<Pageable<BranchListDetailDto>>(new Pageable<BranchListDetailDto>(items, request.PageIndex, request.PageSize, total));
    }

    public async Task<BranchOffice> GetBranchById(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return (await dbContext.BranchOffices.FirstOrDefaultAsync(x => x.Id == id))!;
    }

    public async Task<IDataResult<List<BranchOfficePageListDto>>> GetPageBranchListAsync()
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        // Ana query — scalar sub-query'ler EF Core translate edebilir
        var items = await dbContext.BranchOffices
            .AsNoTracking()
            .Where(b => !b.IsDeleted)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BranchOfficePageListDto(
                b.Id,
                b.Name ?? "",
                dbContext.Users.Count(u => u.DefaultBranchOfficeId == b.Id),
                (int)(dbContext.BranchOfficeStocks
                    .Where(s => s.BranchOfficeId == b.Id)
                    .Sum(s => (int?)(s.FirstTotalStock - s.SoldQuantity)) ?? 0),
                dbContext.MarketPlaceWarehouses.Count(w => w.BranchOfficeId == b.Id && !w.IsDeleted),
                b.CreatedAt,
                b.IsDefaultMarketPlaceStock,
                b.IsHeadquarters))
            .ToListAsync();

        // Marketplace isimlerini ayrı çek (IEnumerable<string> EF projection'da translate edilemiyor)
        if (items.Count > 0)
        {
            var branchIds = items.Select(i => i.Id).ToList();
            var mpNames = await dbContext.MarketPlaceWarehouses
                .Where(w => branchIds.Contains(w.BranchOfficeId) && !w.IsDeleted)
                .Select(w => new { w.BranchOfficeId, Name = w.MarketPlace.Name ?? "" })
                .ToListAsync();

            var nameMap = mpNames.GroupBy(w => w.BranchOfficeId)
                .ToDictionary(g => g.Key, g => g.Select(w => w.Name).ToList());

            foreach (var item in items)
                if (nameMap.TryGetValue(item.Id, out var names))
                    item.MarketPlaceNames.AddRange(names);
        }

        return new SuccessDataResult<List<BranchOfficePageListDto>>(items);
    }

    public async Task<IDataResult<List<BranchStockItemDto>>> GetBranchStocksAsync(int branchId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var stocks = await dbContext.BranchOfficeStocks
            .AsNoTracking()
            .Where(s => s.BranchOfficeId == branchId)
            .Select(s => new BranchStockItemDto(
                s.ProductVariantId!.Value,
                s.ProductVariant!.Product.Title,
                s.ProductVariant.Barcode ?? string.Empty,
                s.FirstTotalStock,
                s.SoldQuantity,
                s.FirstTotalStock - s.SoldQuantity))
            .ToListAsync();
        return new SuccessDataResult<List<BranchStockItemDto>>(stocks);
    }

    public async Task<IDataResult<List<StockMovementViewDto>>> GetBranchStockMovementsAsync(
        int branchId, DateTimeOffset? from, DateTimeOffset? to)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var query = dbContext.StockMovements
            .AsNoTracking()
            .Where(m => m.BranchOfficeId == branchId);

        if (from.HasValue)
            query = query.Where(m => m.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(m => m.CreatedAt <= to.Value);

        var movements = await query
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new StockMovementViewDto(
                m.Id,
                m.CreatedAt,
                m.ProductVariant.Product.Title,
                m.ProductVariant.Barcode ?? string.Empty,
                m.Type,
                m.Quantity,
                m.StockBefore,
                m.StockAfter,
                m.ReferenceType,
                m.ReferenceId))
            .ToListAsync();
        return new SuccessDataResult<List<StockMovementViewDto>>(movements);
    }

    public async Task<IDataResult<List<MarketPlaceWarehouse>>> GetBranchMarketPlacesAsync(int branchId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var warehouses = await dbContext.MarketPlaceWarehouses
            .AsNoTracking()
            .Where(w => w.BranchOfficeId == branchId)
            .Include(w => w.MarketPlace)
            .ToListAsync();
        return new SuccessDataResult<List<MarketPlaceWarehouse>>(warehouses);
    }

    public async Task<IResult> AddMarketPlaceWarehouseAsync(int branchId, int marketPlaceId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var existing = await dbContext.MarketPlaceWarehouses
            .AnyAsync(w => w.BranchOfficeId == branchId && w.MarketPlaceId == marketPlaceId);
        if (existing)
            return new ErrorResult("Bu depo zaten bu pazaryerine bağlı.");

        var warehouse = new MarketPlaceWarehouse
        {
            BranchOfficeId = branchId,
            MarketPlaceId = marketPlaceId
        };
        dbContext.MarketPlaceWarehouses.Add(warehouse);
        await dbContext.SaveChangesAsync();
        return new SuccessResult("Pazaryeri bağlantısı eklendi.");
    }

    public async Task<IResult> RemoveMarketPlaceWarehouseAsync(int warehouseId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var warehouse = await dbContext.MarketPlaceWarehouses.AsTracking()
            .FirstOrDefaultAsync(w => w.Id == warehouseId);
        if (warehouse is null)
            return new ErrorResult("Bağlantı bulunamadı.");

        dbContext.MarketPlaceWarehouses.Remove(warehouse);
        await dbContext.SaveChangesAsync();
        return new SuccessResult("Pazaryeri bağlantısı kaldırıldı.");
    }
}
