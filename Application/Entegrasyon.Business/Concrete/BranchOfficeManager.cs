using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Requests;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Shared.Entity;
using Shared.Logic;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class BranchOfficeManager(
    IntegrationDbContext dbContext,
    IFluentValidator validator,
    IApplicationLogManager applicationLogManager,
    IMapper mapper)
    : IBranchOfficeManager
{
//TODO dto oluşturulacak
private readonly DbSet<BranchOffice> branchOffices = dbContext.BranchOffices;
    public async Task<IDataResult<List<BranchOffice>>> GetBranchList(CancellationToken token=default) =>
        new SuccessDataResult<List<BranchOffice>>(await branchOffices.AsNoTracking().ToListAsync(token));

    public async Task<IDataResult<BranchOffice>> AddBranch(BranchOfficeAddDto officeDto)
    {
        //VALİDATE
        await applicationLogManager.AddLog("Ofis ekleme işlemi yapılmakta.",LogType.Branch,LogAction.Add);
        var result = LogicRunner.Run(await CheckIfTheSameNameExists(officeDto.Name, 0));
        if (result!=null)
            return new ErrorDataResult<BranchOffice>(null, result.Message);
        var office = mapper.Map<BranchOffice>(officeDto);
        await branchOffices.AddAsync(office);
        await dbContext.SaveChangesAsync();
        await applicationLogManager.AddLog($"Ofis ekleme işlemi başarıyla tamamlandı. {office.Name}",LogType.Branch,LogAction.Add);
        return new SuccessDataResult<BranchOffice>(office,Messages.BranchAdded);
    }

    public async Task<IDataResult<BranchDetailDto>> GetBranchDetailById(int branchId)
    {
        var data = await branchOffices.Select(b =>
            new BranchDetailDto(b.Id, b.Name, b.Users.Count(), b.CreatedAt))
            .FirstOrDefaultAsync(b=>b.Id==branchId);
        return new SuccessDataResult<BranchDetailDto>(data);
    }

    public async Task<IDataResult<BranchOffice>> Update(BranchOfficeEditDto dto)
    {
        //validate
        //TODO update için rowversion ekle
        await applicationLogManager.AddLog("Ofis düzenleme işlemi yapılmakta.",LogType.Branch,LogAction.Update,dto);
        var result = LogicRunner.Run(await CheckIfTheSameNameExists(dto.Name, dto.Id));
        if (result != null)
            return new ErrorDataResult<BranchOffice>(null, result.Message);
        var dbOffice = await branchOffices.AsTracking().FirstOrDefaultAsync(x => x.Id == dto.Id);
        dbOffice.Name = dto.Name;
        await dbContext.SaveChangesAsync();
        await applicationLogManager.AddLog("Ofis düzenleme işlemi başarıyla tamamlandı. ",LogType.Branch,LogAction.Update);
        return new SuccessDataResult<BranchOffice>(dbOffice);
    }
    public async Task<IResult> Delete(int id)
    {
        await applicationLogManager.AddLog("Ofis silme işlemi yapılmakta.",LogType.Branch,LogAction.Delete);
        if (id <= 0)
        {
            return new ErrorResult("Ofis id si alırken sıkıntı yaşanmıştır.");
        }
        branchOffices.Remove(new BranchOffice(){Id=id});
        await dbContext.SaveChangesAsync();
        await applicationLogManager.AddLog("Ofis silme işlemi başarıyla tamamlandı. ",LogType.Branch,LogAction.Delete);
        return new SuccessResult("Ofis silme işlemi başarıyla tamamlandı. ");
    }

    private async Task<IResult> CheckIfTheSameNameExists(string name,int id)
    {
        bool exits = await branchOffices.AnyAsync(b => string.Equals(name, b.Name) && b.Id != id);
        return exits ? new ErrorResult(Messages.OfficeNameAlreadyExists) : new SuccessResult();
    }

    public async Task<bool> CheckIfOfficesExits(IEnumerable<int> officeIds)
    {
        var officeIdArray = officeIds.ToArray();
        var existingCount = await branchOffices.CountAsync(o => officeIdArray.Contains(o.Id));
        return existingCount == officeIdArray.Length;
    }

    public async Task<IDataResult<Pageable<BranchListDetailDto>>> GetPageableBranchOffices(int pageIndex = 0, int pageSize = 50)
    {
        int total = await branchOffices.CountAsync();
        var items = await branchOffices
            .OrderByDescending(b => b.CreatedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(b => new BranchListDetailDto(b.CreatedAt, b.Id, b.Name, b.Users.Count()))
            .ToListAsync();
        return new SuccessDataResult<Pageable<BranchListDetailDto>>(new Pageable<BranchListDetailDto>(items, pageIndex, pageSize, total));
    }

    public async Task<IDataResult<Pageable<BranchListDetailDto>>> GetPageableBranchOffices(BranchPaginatedRequest request)
    {
        int total = await branchOffices.CountAsync();
        var items = await branchOffices
            .OrderByDescending(b => b.CreatedAt)
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(b => new BranchListDetailDto(b.CreatedAt, b.Id, b.Name, b.Users.Count()))
            .ToListAsync();
        return new SuccessDataResult<Pageable<BranchListDetailDto>>(new Pageable<BranchListDetailDto>(items, request.PageIndex, request.PageSize, total));
    }

    public Task<BranchOffice> GetBranchById(int id) =>
        branchOffices.FirstOrDefaultAsync(x => x.Id == id);
}
