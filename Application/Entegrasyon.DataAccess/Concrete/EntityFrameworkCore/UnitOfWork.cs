using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly CancellationToken _cancellationToken;
    private readonly IntegrationDbContext _dbContext;
    private IDbContextTransaction _currentTransaction;
    private bool _disposed;

    public UnitOfWork(IntegrationDbContext dbContext,IHttpContextAccessor accessor)
    {
        _dbContext = dbContext;
        _cancellationToken = accessor.HttpContext?.RequestAborted ?? CancellationToken.None;
    }
    public async Task BeginTransactionAsync()
    {
        CheckTransaction();
        _currentTransaction = await _dbContext.Database.BeginTransactionAsync(_cancellationToken);
    }

    public async Task CommitAsync()
    {
        CheckDisposed();
        await _currentTransaction.CommitAsync(_cancellationToken);
    }
    public async ValueTask DisposeAsync()
    {
        if(!_disposed)
        {
            if(_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
    public async Task RollBackAsync()
    {
        CheckDisposed();
        await _currentTransaction.RollbackAsync(_cancellationToken);
    }

    public Task SaveAsync()=>_dbContext.SaveChangesAsync(_cancellationToken);
    

    private void CheckDisposed()
    {
        if(_disposed)
            throw new ObjectDisposedException(nameof(_currentTransaction));
    }
    private void CheckStatus()
    {
        CheckDisposed();
        CheckTransaction();
    }
    private void CheckTransaction()
    {
        if(_currentTransaction != null)
            throw new InvalidOperationException("Transaction has already been started.");
    }
}