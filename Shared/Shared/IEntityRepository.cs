using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Shared.Entity;

namespace Shared;

public interface IEntityRepository<T>
where T : class, new()
{
    DbSet<T> Table { get; }
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<T> Get(Expression<Func<T, bool>> expression, bool isTracking = false);
    Task<List<T>> GetAllAsync(Expression<Func<T, bool>> expression = null, bool isTracking = false);
    Task<Pageable<T>> GetAllPageableAsync(int page,int pageSize,Expression<Func<T,bool>> expression = null);

    Task<bool> Exists(Expression<Func<T, bool>>? expression = null);

}