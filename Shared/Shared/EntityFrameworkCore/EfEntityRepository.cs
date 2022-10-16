using System.Linq.Expressions;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
    public async Task AddAsync(TEntity entity)
    {
        _context.Set<TEntity>().Add(entity);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateAsync(TEntity entity)
    {
        _context.Set<TEntity>().Update(entity);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeleteAsync(TEntity entity)
    {
        _context.Set<TEntity>().Remove(entity);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<TEntity> Get(Expression<Func<TEntity,bool>> expression,bool isTracking = false)
    {
        return (isTracking
            ? await _context.Set<TEntity>().FirstOrDefaultAsync(expression)
            : await _context.Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(expression))!;
    }

    public async Task<List<TEntity>> GetAllAsync(Expression<Func<TEntity,bool>> expression = null,bool isTracking = false)
    {
        var entities = isTracking ? _context.Set<TEntity>() : _context.Set<TEntity>().AsNoTracking();
        return expression == null ? await entities.ToListAsync() : await entities.Where(expression).ToListAsync();
    }
    public async Task<TResult> GetTransformedEntity<TResult>(
        Expression<Func<TEntity,TResult>> selector,
        IEnumerable<(bool, Expression<Func<TEntity,bool>>)> expressionTuples = null)
    {
        var entities = _context.Set<TEntity>().AsNoTracking();
        entities = entities.ApplyFilter(expressionTuples);
        return await entities.Select(selector).FirstOrDefaultAsync();
    }
    public IQueryable<TResult> GetTransformedEntities<TResult>(
        Expression<Func<TEntity,TResult>> selector,
        IEnumerable<(string,string)> orderTuples = null,
        IEnumerable<(bool, Expression<Func<TEntity,bool>>)> expressionTuples = null
        )
    {
        var entities =_context.Set<TEntity>().AsNoTracking();
        entities = entities.OrderQueryableDynamicly(orderTuples);
        entities = entities.ApplyFilter(expressionTuples);
        return entities.Select(selector).AsQueryable();
    }
    public async Task<Pageable<TResult>> GetPaginatedTransformedEntities<TResult>(
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity,TResult>> selector,
        IEnumerable<(string,string)> orderTuples = null,
        IEnumerable<(bool, Expression<Func<TEntity,bool>>)> expressionTuples = null)
    {
        return await GetTransformedEntities(selector,orderTuples,expressionTuples).ToPagable(pageIndex,pageSize);
    }
    
    public async Task<bool> Exists(Expression<Func<TEntity,bool>>? expression = null)
    {
        return expression == null
            ? await _context.Set<TEntity>().AnyAsync()
            : await _context.Set<TEntity>().AnyAsync(expression);
    }


}