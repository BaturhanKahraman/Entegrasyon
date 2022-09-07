using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity;
using FluentValidation;
using Shared.Constants;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class BranchOfficeManager
{
    private readonly IBranchOfficeDal _branchOfficeDal;
    private readonly IValidator<BranchOffice> _validator;
    public BranchOfficeManager(IBranchOfficeDal branchOfficeDal, IValidator<BranchOffice> validator)
    {
        _branchOfficeDal = branchOfficeDal;
        _validator = validator;
    }

    public  async Task<IDataResult<List<BranchOffice>>> GetBranchList()=>
        new SuccessDataResult<List<BranchOffice>>(await _branchOfficeDal.GetAllAsync());

    public async Task<IResult> AddBranch(BranchOffice office)
    {
        await _validator.ValidateAndThrowAsync(office);
        await _branchOfficeDal.AddAsync(office);
        return new SuccessResult(Messages.BranchAdded);
    }
}