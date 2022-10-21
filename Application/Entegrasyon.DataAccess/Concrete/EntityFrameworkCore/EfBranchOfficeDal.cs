using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Microsoft.EntityFrameworkCore;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfBranchOfficeDal:EfEntityRepository<BranchOffice,IntegrationDbContext>,IBranchOfficeDal
{
    private readonly IntegrationDbContext _dbContext;
    public EfBranchOfficeDal(IntegrationDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> CheckIfOfficesExits(IEnumerable<int> stocks)
    {
        return await _dbContext.BranchOffices.AllAsync(x=>stocks.Contains(x.Id));
    }
    
}