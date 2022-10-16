using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Logs;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Extensions;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class BranchOfficeManager
{
    private readonly IBranchOfficeDal _branchOfficeDal;
    private readonly FluentValidator _validator;
    private readonly ApplicationLogManager _applicationLogManager;
    public BranchOfficeManager(IBranchOfficeDal branchOfficeDal,FluentValidator validator,ApplicationLogManager applicationLogManager)
    {
        _branchOfficeDal = branchOfficeDal;
        _validator = validator;
        _applicationLogManager = applicationLogManager;
    }

    public async Task<IDataResult<List<BranchOffice>>> GetBranchList() =>
        new SuccessDataResult<List<BranchOffice>>(await _branchOfficeDal.GetAllAsync());

    public async Task<IResult> AddBranch(BranchOffice office)
    {
        await _applicationLogManager.AddLog("Ofis ekleme işlemi yapılmakta.",LogType.Branch,LogAction.Add);
        await _validator.ValidateAndThrowAsync(office);
        office.CreatedAt = DateTimeOffset.UtcNow;
        await _branchOfficeDal.AddAsync(office);
        await _applicationLogManager.AddLog($"Ofis ekleme işlemi başarıyla tamamlandı. {office.Name}",LogType.Branch,LogAction.Add);
        return new SuccessResult(Messages.BranchAdded);
    }

    public async Task<IDataResult<BranchDetailDto>> GetBranchDetailById(int branchId)
    {
        var data = await _branchOfficeDal.Table
            .Include(x => x.Users)
            .Where(x => x.Id == branchId).Select(x => new BranchDetailDto()
            {
                CreatedAt = x.CreatedAt,
                Id = x.Id,
                Name = x.Name,
                UserCount = x.Users.Count
            }).FirstOrDefaultAsync();
        return new SuccessDataResult<BranchDetailDto>(data);
    }

    public async Task<IDataResult<BranchOffice>> Update(BranchOffice branchOffice)
    {
        await _validator.ValidateAndThrowAsync(branchOffice);
        await _applicationLogManager.AddLog("Ofis düzenleme işlemi yapılmakta.",LogType.Branch,LogAction.Update);
        await _branchOfficeDal.UpdateAsync(branchOffice);
        await _applicationLogManager.AddLog("Ofis düzenleme işlemi başarıyla tamamlandı. ",LogType.Branch,LogAction.Update);
        return new SuccessDataResult<BranchOffice>(branchOffice);
    }
    public async Task<IResult> Delete(int id)
    {
        await _applicationLogManager.AddLog("Ofis silme işlemi yapılmakta.",LogType.Branch,LogAction.Delete);
        if (id <= 0)
        {
            return new ErrorResult("Ofis id si alırken sıkıntı yaşanmıştır.");
        }
        await _branchOfficeDal.DeleteAsync(new BranchOffice(){Id=id});
        await _applicationLogManager.AddLog("Ofis silme işlemi başarıyla tamamlandı. ",LogType.Branch,LogAction.Delete);
        return new SuccessResult("Ofis silme işlemi başarıyla tamamlandı. ");
    }


    public async Task<bool> CheckIfOfficesExits(int[] officeIds)
    {
        return await _branchOfficeDal.CheckIfOfficesExits(officeIds);
    }
}