using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Shared.Entity;
using Shared.Extensions;

namespace Shared.EntityFrameworkCore;

public class EfEntityRepository<TEntity, TContext> : IEntityRepository<TEntity>
where TEntity : class, new()
where TContext : DbContext
{
    private readonly TContext _context;
    public EfEntityRepository(TContext ctx)
    {
        _context = ctx;
        Table = _context.Set<TEntity>();
    }
    public DbSet<TEntity> Table { get; }
    protected async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task AddAsync(TEntity entity)
    {
        await Table.AddAsync(entity).ConfigureAwait(false);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateAsync(TEntity entity)
    {
        Table.Update(entity);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeleteAsync(TEntity entity)
    {
        Table.Remove(entity);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task RemoveRangeAsync(IEnumerable<TEntity> entities)
    {
        Table.RemoveRange(entities);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<TEntity> GetAsync(Expression<Func<TEntity,bool>> expression,bool isTracking = false)
    {
        return (isTracking
            ? await Table.FirstOrDefaultAsync(expression)
            : await Table.AsNoTracking().FirstOrDefaultAsync(expression))!;
    }

    public async Task<List<TEntity>> GetAllAsync(Expression<Func<TEntity,bool>> expression = null,bool isTracking = false)
    {
        var entities = isTracking ? Table : Table.AsNoTracking();
        var result =expression == null ? entities :  entities.Where(expression);
        return await result.ToListAsync().ConfigureAwait(false);
    }
    public async Task<TResult> GetTransformedEntity<TResult>(
        Expression<Func<TEntity,TResult>> selector,
        Expression<Func<TEntity,bool>> expression=null)
    {
        var entities = Table.AsNoTracking();
        entities = entities.ApplyFilter(expression);
        return await entities.Select(selector).FirstOrDefaultAsync().ConfigureAwait(false);
    }
    public Task<List<TResult>> GetTransformedEntitiesAsync<TResult>(
        Expression<Func<TEntity,TResult>> selector,
        IEnumerable<(string,string)> orderTuples = null,
        Expression<Func<TEntity,bool>> expression = null
        )
    {
        return GetTransformedEntitiesAsQueryable(selector,orderTuples,expression).ToListAsync();
    }
    public async Task<Pageable<TResult>> GetPaginatedTransformedEntities<TResult>(
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity,TResult>> selector,
        IEnumerable<(string,string)> orderTuples = null,
        Expression<Func<TEntity,bool>> expression = null)
    {
        return await GetTransformedEntitiesAsQueryable(selector,orderTuples,expression).ToPage(pageIndex,pageSize).ConfigureAwait(false);
    }
    
    public async Task<bool> Exists(Expression<Func<TEntity,bool>> expression = null)
    {
        return expression == null
            ? await Table.AnyAsync().ConfigureAwait(false)
            : await Table.AnyAsync(expression).ConfigureAwait(false);
    }
    private IQueryable<TResult> GetTransformedEntitiesAsQueryable<TResult>(
       Expression<Func<TEntity, TResult>> selector,
       IEnumerable<(string, string)> orderTuples = null,
       Expression<Func<TEntity, bool>> expression = null
       )
    {
        var entities = Table.AsNoTracking();
        entities = entities.OrderQueryableDynamicly(orderTuples);
        entities = entities.ApplyFilter(expression);
        return entities.Select(selector);
    }

}