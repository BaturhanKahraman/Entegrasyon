using Microsoft.EntityFrameworkCore;
using Shared.EntityFrameworkCore;
using System.Linq.Expressions;
using Shared.Entity;

namespace Shared.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<TEntity> ApplyFilter<TEntity>(this IQueryable<TEntity> @this,Expression<Func<TEntity,bool>> filterExpression) 
        => filterExpression == null ? @this : @this.Where(filterExpression);

    public static IQueryable<TEntity> OrderQueryableDynamicly<TEntity>(this IQueryable<TEntity> @this,IEnumerable<(string, string)> sortTuples)
    {
        if(sortTuples == null)
            return @this;
        sortTuples = sortTuples.ToList();
        var result = CreateExpression(sortTuples.First().Item1, sortTuples.First().Item2, "orderDirection", @this);
        @this = @this.Provider.CreateQuery<TEntity>(result);
        for (int i = 1; i < sortTuples.Count(); i++)
        {
            var resultExp = CreateExpression(sortTuples.First().Item1,sortTuples.First().Item2,"then",@this);
            @this = @this.Provider.CreateQuery<TEntity>(resultExp);
        }
        return @this;
    }

    private static MethodCallExpression CreateExpression<TEntity>(string prop, string orderDirection, string orderByOrThenBy,IQueryable<TEntity> queryable)
    {
        var expressionParam = Expression.Parameter(typeof(TEntity),"x");
        Expression expressionProperty;
        if(prop.Contains('.'))
        {
            var props = prop.Split('.');
            expressionProperty = Expression.Property(expressionParam,props[0]);
            for(var i = 1;i < props.Length;i++)
            {
                expressionProperty = Expression.Property(expressionProperty,props[i]);
            }
        }
        else
            expressionProperty = Expression.Property(expressionParam,prop);
        var expression = Expression.Lambda(expressionProperty,expressionParam);
        string method = string.Empty;
        if (orderByOrThenBy == "orderDirection")
            method += "OrderBy";
        else
            method += "ThenBy";
        if (string.Equals(orderDirection,"asc",StringComparison.OrdinalIgnoreCase))
            method += "Descending";
        return Expression.Call(typeof(Queryable),method,new[] { typeof(TEntity),expressionProperty.Type },queryable.Expression,Expression.Quote(expression));
    }

    public static IQueryable<TEntity> OrderQueryable<TEntity>(this IQueryable<TEntity> @this,IEnumerable<(OrderDirection, Expression<Func<TEntity,object>>)> orderTuples)
    {
        if (orderTuples == null)
            return @this;
        var valueTuples = orderTuples.ToList();
        if(!valueTuples.Any())
            return @this;
        @this = valueTuples[0].Item1 == OrderDirection.Ascending
            ? @this.OrderBy(valueTuples[0].Item2)
            : @this.OrderByDescending(valueTuples[0].Item2);
        for(var i = 1;i < valueTuples.Count;i++)
        {
            @this = valueTuples[i].Item1 == OrderDirection.Ascending
                ? ((IOrderedQueryable<TEntity>)@this).ThenBy(valueTuples[i].Item2)
                : ((IOrderedQueryable<TEntity>)@this).ThenByDescending(valueTuples[i].Item2);
        }
        return @this;
    }

    public static async Task<Pageable<TEntity>> ToPage<TEntity>(this IQueryable<TEntity> @this,int pageIndex,int pageSize)
    {
        var items =await @this.ToPageableQuery(pageIndex, pageSize).ToListAsync();
        int totalItemCount = await @this.CountAsync();
        return new Pageable<TEntity>(items,pageIndex,pageSize,totalItemCount);
    }
    public static IQueryable<TEntity> ToPageableQuery<TEntity>(this IQueryable<TEntity> @this, int pageIndex, int pageSize)
    {
        if (@this == null)
            return null;
        if (pageIndex < 0)
            throw new ArgumentException("Page index must be greater than or equal to 0");
        if (pageSize <= 0)
            throw new ArgumentException("Page size must be greater than 0");
        return @this.Skip(pageIndex * pageSize).Take(pageSize);
    }

    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query,bool condition,Expression<Func<T,bool>> predicate)
        => condition ? query.Where(predicate) : query;
}