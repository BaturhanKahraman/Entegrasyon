using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Requests;
using Entegrasyon.Entity.POS;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Concrete;

public class BranchOfficeManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IApplicationLogManager applicationLogManager,
    BranchOfficeMapper mapper)
    : IBranchOfficeManager
{
    public async Task<IDataResult<List<BranchOffice>>> GetBranchList(CancellationToken token=default)
    {
        using var dbContext = contextFactory.CreateDbContext();
        return new SuccessDataResult<List<BranchOffice>>(await dbContext.BranchOffices.AsNoTracking().ToListAsync(token));
    }

    public async Task<IDataResult<BranchOffice>> AddBranch(BranchOfficeAddDto officeDto)
    {
        using var dbContext = contextFactory.CreateDbContext();
        //VALİDATE
        await applicationLogManager.AddLog("Ofis ekleme işlemi yapılmakta.",LogType.Branch,LogAction.Add);
        var result = LogicRunner.Run(await CheckIfTheSameNameExists(dbContext, officeDto.Name, 0));
        if (result!=null)
            return new ErrorDataResult<BranchOffice>(null!, result.Message!);
        var office = mapper.MapToEntity(officeDto);
        await dbContext.BranchOffices.AddAsync(office);
        await dbContext.SaveChangesAsync();
        await applicationLogManager.AddLog($"Ofis ekleme işlemi başarıyla tamamlandı. {office.Name}",LogType.Branch,LogAction.Add);
        return new SuccessDataResult<BranchOffice>(office,Messages.BranchAdded);
    }

    public async Task<IDataResult<BranchDetailDto>> GetBranchDetailById(int branchId)
    {
        using var dbContext = contextFactory.CreateDbContext();
        var data = await dbContext.BranchOffices.Select(b =>
            new BranchDetailDto(b.Id, b.Name!, b.Users.Count(), b.CreatedAt))
            .FirstOrDefaultAsync(b=>b.Id==branchId);
        return new SuccessDataResult<BranchDetailDto>(data!);
    }

    public async Task<IDataResult<BranchOffice>> Update(BranchOfficeEditDto dto)
    {
        using var dbContext = contextFactory.CreateDbContext();
        //validate
        //TODO update için rowversion ekle
        await applicationLogManager.AddLog("Ofis düzenleme işlemi yapılmakta.",LogType.Branch,LogAction.Update,dto);
        var result = LogicRunner.Run(await CheckIfTheSameNameExists(dbContext, dto.Name, dto.Id));
        if (result != null)
            return new ErrorDataResult<BranchOffice>(null!, result.Message!);
        var dbOffice = (await dbContext.BranchOffices.AsTracking().FirstOrDefaultAsync(x => x.Id == dto.Id))!;
        dbOffice.Name = dto.Name;
        await dbContext.SaveChangesAsync();
        await applicationLogManager.AddLog("Ofis düzenleme işlemi başarıyla tamamlandı. ",LogType.Branch,LogAction.Update);
        return new SuccessDataResult<BranchOffice>(dbOffice);
    }
    public async Task<IResult> Delete(int id)
    {
        using var dbContext = contextFactory.CreateDbContext();
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

    public async Task<bool> CheckIfOfficesExits(IEnumerable<int> officeIds)
    {
        using var dbContext = contextFactory.CreateDbContext();
        var officeIdArray = officeIds.ToArray();
        var existingCount = await dbContext.BranchOffices.CountAsync(o => officeIdArray.Contains(o.Id));
        return existingCount == officeIdArray.Length;
    }

    public async Task<IDataResult<Pageable<BranchListDetailDto>>> GetPageableBranchOffices(int pageIndex = 0, int pageSize = 50)
    {
        using var dbContext = contextFactory.CreateDbContext();
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
        using var dbContext = contextFactory.CreateDbContext();
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
        using var dbContext = contextFactory.CreateDbContext();
        return (await dbContext.BranchOffices.FirstOrDefaultAsync(x => x.Id == id))!;
    }

    public async Task<IDataResult<List<BranchOfficePageListDto>>> GetPageBranchListAsync()
    {
        using var dbContext = contextFactory.CreateDbContext();
        var items = await dbContext.BranchOffices
            .AsNoTracking()
            .Where(b => !b.IsDeleted)
            .Select(b => new BranchOfficePageListDto(
                b.Id,
                b.Name ?? "",
                dbContext.Users.Count(u => u.DefaultBranchOfficeId == b.Id),
                dbContext.BranchOfficeStocks
                    .Where(s => s.BranchOfficeId == b.Id)
                    .Select(s => s.FirstTotalStock - s.SoldQuantity)
                    .DefaultIfEmpty(0)
                    .Sum(),
                dbContext.MarketPlaceWarehouses
                    .Where(w => w.BranchOfficeId == b.Id && !w.IsDeleted)
                    .Select(w => w.MarketPlace.Name ?? "")
                    .ToList(),
                b.CreatedAt,
                b.IsDefaultMarketPlaceStock))
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
        return new SuccessDataResult<List<BranchOfficePageListDto>>(items);
    }

    public async Task<IDataResult<List<BranchStockItemDto>>> GetBranchStocksAsync(int branchId)
    {
        using var dbContext = contextFactory.CreateDbContext();
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
        using var dbContext = contextFactory.CreateDbContext();
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
        using var dbContext = contextFactory.CreateDbContext();
        var warehouses = await dbContext.MarketPlaceWarehouses
            .AsNoTracking()
            .Where(w => w.BranchOfficeId == branchId)
            .Include(w => w.MarketPlace)
            .ToListAsync();
        return new SuccessDataResult<List<MarketPlaceWarehouse>>(warehouses);
    }

    public async Task<IResult> AddMarketPlaceWarehouseAsync(int branchId, int marketPlaceId)
    {
        using var dbContext = contextFactory.CreateDbContext();
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
        using var dbContext = contextFactory.CreateDbContext();
        var warehouse = await dbContext.MarketPlaceWarehouses.AsTracking()
            .FirstOrDefaultAsync(w => w.Id == warehouseId);
        if (warehouse is null)
            return new ErrorResult("Bağlantı bulunamadı.");

        dbContext.MarketPlaceWarehouses.Remove(warehouse);
        await dbContext.SaveChangesAsync();
        return new SuccessResult("Pazaryeri bağlantısı kaldırıldı.");
    }
}
