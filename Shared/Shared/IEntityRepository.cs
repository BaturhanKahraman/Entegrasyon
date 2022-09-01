using System.Linq.Expressions;

namespace Shared;

public interface IEntityRepository<T>
where T : class, new()
{
    IQueryable Table { get; }
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<T> Get(Expression<Func<T, bool>> expression, bool isTracking = false);
    Task<List<T>> GetAllAsync(Expression<Func<T, bool>> expression = null, bool isTracking = false);
    Task<List<T>> GetAllPageableAsync(Expression<Func<T, bool>> expression, int page, int pageSize, bool isTracking = false);

    Task<bool> Exists(Expression<Func<T, bool>>? expression = null);

}