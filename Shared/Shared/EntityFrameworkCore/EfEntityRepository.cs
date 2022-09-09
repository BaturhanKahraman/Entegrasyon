using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

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
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TEntity entity)
    {
        _context.Set<TEntity>().Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(TEntity entity)
    {
        _context.Set<TEntity>().Remove(entity);
        await _context.SaveChangesAsync();
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
    public async Task<List<TEntity>> GetAllPageableAsync(Expression<Func<TEntity,bool>> expression,int page,int pageSize,bool isTracking = false)
    {
        return await _context.Set<TEntity>().Where(expression).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<bool> Exists(Expression<Func<TEntity,bool>>? expression = null)
    {
        return expression == null
            ? await _context.Set<TEntity>().AnyAsync()
            : await _context.Set<TEntity>().AnyAsync(expression);
    }


}