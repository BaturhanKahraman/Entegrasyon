using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Shared.Entity;
using Shared.EntityFrameworkCore;
using static Amazon.S3.Util.S3EventNotification;

namespace Shared;

public interface IEntityRepository<T>
where T : class, new()
{
    DbSet<T> Table { get; }
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<T> GetAsync(Expression<Func<T,bool>> expression,bool isTracking = false);
    Task<List<T>> GetAllAsync(Expression<Func<T,bool>> expression = null,bool isTracking = false);

    Task<bool> Exists(Expression<Func<T,bool>>? expression = null);

    IQueryable<TResult> GetTransformedEntities<TResult>(
        Expression<Func<T,TResult>> selector,
        IEnumerable<(string, string)> orderTuples = null,
        Expression<Func<T,bool>> expression= null);

    Task<Pageable<TResult>> GetPaginatedTransformedEntities<TResult>(
        int pageIndex,
        int pageSize,
        Expression<Func<T,TResult>> selector,
        IEnumerable<(string, string)> orderTuples = null,
        Expression<Func<T,bool>> expression = null);

    Task<TResult> GetTransformedEntity<TResult>(
    Expression<Func<T,TResult>> selector,
        Expression<Func<T,bool>> expression = null);
}