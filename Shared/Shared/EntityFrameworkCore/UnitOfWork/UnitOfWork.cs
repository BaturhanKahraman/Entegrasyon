using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;

namespace Shared.EntityFrameworkCore.UnitOfWork;

public class UnitOfWork:IUnitOfWork
{
    private readonly DbContext _dbContext;
    private IDbContextTransaction _transaction;

    public UnitOfWork(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task BeginTransaction()
    {
        _transaction = await _dbContext.Database.BeginTransactionAsync();
    } 

    public Task Commit()=>_transaction.CommitAsync();

    public Task Rollback() => _transaction.RollbackAsync();
    public void Dispose()
    {
        _transaction?.Dispose();
        _dbContext?.Dispose();
    }

  
}