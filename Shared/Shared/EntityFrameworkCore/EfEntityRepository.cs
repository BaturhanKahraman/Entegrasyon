using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Shared.Entity;

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
    public async Task<Pageable<TEntity>> GetAllPageableAsync(int page,int pageSize,bool isTracking = false,Expression<Func<TEntity,bool>> expression = null)
    {
        var entities = isTracking ? _context.Set<TEntity>() : _context.Set<TEntity>().AsNoTracking();
        var resultEntities = expression==null ?  
            await entities.Skip((page-1)*pageSize).Take(pageSize)
            .ToListAsync():
            await entities.Where(expression).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var totalItemCount = entities.Count();
        var pageCount =Convert.ToInt32(Math.Round(totalItemCount / (double)pageSize));
        return new Pageable<TEntity> { CurrentPage = page,PagingItemCount = pageSize,Items = resultEntities,TotalPageCount = pageCount,TotalItemCount=totalItemCount };
    }

    public async Task<bool> Exists(Expression<Func<TEntity,bool>>? expression = null)
    {
        return expression == null
            ? await _context.Set<TEntity>().AnyAsync()
            : await _context.Set<TEntity>().AnyAsync(expression);
    }


}