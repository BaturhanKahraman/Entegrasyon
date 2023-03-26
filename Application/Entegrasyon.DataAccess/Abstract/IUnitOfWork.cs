using System.Data;

namespace Entegrasyon.DataAccess.Abstract;

public interface IUnitOfWork
{
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollBackAsync();
    Task SaveAsync();
}