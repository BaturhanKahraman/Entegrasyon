namespace Shared.EntityFrameworkCore.UnitOfWork;

public interface IUnitOfWork:IDisposable
{
    Task BeginTransaction();
    Task Commit();
    Task Rollback();
}