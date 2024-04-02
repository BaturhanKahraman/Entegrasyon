using AutoMapper;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Branches;
using Entegrasyon.Entity.Logs;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Entity;
using Shared.Logic;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class BranchOfficeManager
{
    private readonly IBranchOfficeDal _branchOfficeDal;
    private readonly FluentValidator _validator;
    private readonly ApplicationLogManager _applicationLogManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    public BranchOfficeManager(IBranchOfficeDal branchOfficeDal,FluentValidator validator,ApplicationLogManager applicationLogManager, IMapper mapper, IUnitOfWork unitOfWork)
    {
        _branchOfficeDal = branchOfficeDal;
        _validator = validator;
        _applicationLogManager = applicationLogManager;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<IDataResult<List<BranchOffice>>> GetBranchList(CancellationToken token=default) =>
        new SuccessDataResult<List<BranchOffice>>(await _branchOfficeDal.GetAllAsync(token:token));

    public async Task<IDataResult<BranchOffice>> AddBranch(BranchOfficeAddDto officeDto)
    {
        //VALİDATE
        await _applicationLogManager.AddLog("Ofis ekleme işlemi yapılmakta.",LogType.Branch,LogAction.Add);
        var result = LogicRunner.Run(await CheckIfTheSameNameExists(officeDto.Name, 0));
        if (result!=null)
            return new ErrorDataResult<BranchOffice>(null, result.Message);
        var office = _mapper.Map<BranchOffice>(officeDto);
        await _branchOfficeDal.AddAsync(office);
        await _applicationLogManager.AddLog($"Ofis ekleme işlemi başarıyla tamamlandı. {office.Name}",LogType.Branch,LogAction.Add);
        return new SuccessDataResult<BranchOffice>(office,Messages.BranchAdded);
    }

    public async Task<IDataResult<BranchDetailDto>> GetBranchDetailById(int branchId)
    {
        var data = await _branchOfficeDal.GetTransformedEntity(b =>
            new BranchDetailDto(b.Id, b.Name, b.Users.Count(), b.CreatedAt),b=>b.Id==branchId);
        return new SuccessDataResult<BranchDetailDto>(data);
    }

    public async Task<IDataResult<BranchOffice>> Update(BranchOfficeEditDto dto)
    {
        //validate
        await _applicationLogManager.AddLog("Ofis düzenleme işlemi yapılmakta.",LogType.Branch,LogAction.Update,dto);
        var result = LogicRunner.Run(await CheckIfTheSameNameExists(dto.Name, dto.Id));
        if (result != null)
            return new ErrorDataResult<BranchOffice>(null, result.Message);
        var dbOffice = await _branchOfficeDal.GetAsync(x => x.Id == dto.Id,true);
        dbOffice.Name = dto.Name;
        await _unitOfWork.SaveAsync();
        await _applicationLogManager.AddLog("Ofis düzenleme işlemi başarıyla tamamlandı. ",LogType.Branch,LogAction.Update);
        return new SuccessDataResult<BranchOffice>(dbOffice);
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

    private async Task<IResult> CheckIfTheSameNameExists(string name,int id)
    {
        bool exits = await _branchOfficeDal.Exists(b => string.Equals(name, b.Name) && b.Id != id);
        return exits ? new ErrorResult(Messages.OfficeNameAlreadyExists) : new SuccessResult();
    }

    public async Task<bool> CheckIfOfficesExits(IEnumerable<int> officeIds)
    {
        return await _branchOfficeDal.CheckIfOfficesExits(officeIds.ToArray());
    }

    public async Task<IDataResult<Pageable<BranchListDetailDto>>> GetPageableBranchOffices(int pageIndex=0,int pageSize=50)
    {
        var result =await _branchOfficeDal.GetPaginatedTransformedEntities(pageIndex, pageSize,
            b => new BranchListDetailDto(b.CreatedAt, b.Id, b.Name, b.Users.Count()),new List<(string, string)>(){new ("CreatedAt","desc")});
        return new SuccessDataResult<Pageable<BranchListDetailDto>>(result);
    }

    public Task<BranchOffice> GetBranchById(int id) => _branchOfficeDal.GetAsync(x => x.Id == id);
}