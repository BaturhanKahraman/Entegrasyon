using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Shared.Entity;
using System.ComponentModel;
using Amazon.Auth.AccessControlPolicy;
using System.Reflection;

namespace Shared.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<TEntity> ApplyFilter<TEntity>(this IQueryable<TEntity> @this, Expression<Func<TEntity, bool>> filterExpression)
        => filterExpression == null ? @this : @this.Where(filterExpression);


    public static Pageable<TEntity> GetPageableByParameters<TEntity>(this IQueryable<TEntity> entities, QueryParameter parameter)
    {
        //ilk filtrele
        //sırala
        //page e dönüştür
        throw new NotImplementedException();
    }
    public static IQueryable<T> ApplySorting<T>(this IQueryable<T> source, IEnumerable<SortingParameter> sortingParameters)
    {
        IOrderedQueryable<T> orderedQuery = null;

        foreach (var param in sortingParameters.OrderBy(x=>x.Order))
        {
            var propertyInfo = typeof(T).GetProperty(param.SortBy, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo == null) continue; // Geçersiz property adı durumu

            var parameter = Expression.Parameter(typeof(T), "x");
            var propertyAccess = Expression.MakeMemberAccess(parameter, propertyInfo);
            var orderByExp = Expression.Lambda(propertyAccess, parameter);

            orderedQuery = orderedQuery == null ?
                (param.OrderDirection == OrderDirection.Descending ? Queryable.OrderByDescending(source, (dynamic)orderByExp) : Queryable.OrderBy(source, (dynamic)orderByExp)) :
                (param.OrderDirection == OrderDirection.Descending ? Queryable.ThenByDescending((dynamic)orderedQuery, (dynamic)orderByExp) : Queryable.ThenBy((dynamic)orderedQuery, (dynamic)orderByExp));
        }

        return orderedQuery ?? source;
    }
    public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> queryable, IEnumerable<FilterParameter> filters)
    {
        if (queryable is null || filters is null || !filters.Any())
            return queryable;
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression combined = null;
        foreach (var filter in filters)
        {
            var property = typeof(T).GetProperty(filter.Field, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (property is null)
                continue;
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var converter = TypeDescriptor.GetConverter(property.PropertyType);
            var value = converter.ConvertFromString(filter.Value);
            var constant = Expression.Constant(value);

            Expression expr = filter.Operator switch
            {
                FilterOperator.Equals => Expression.Equal(propertyAccess, constant),
                FilterOperator.Contains => Expression.Call(propertyAccess, typeof(string).GetMethod("Contains", new[] { typeof(string) }), constant),
                FilterOperator.GreaterThan => Expression.GreaterThan(propertyAccess, constant),
                FilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(propertyAccess, constant),
                FilterOperator.LessThan => Expression.LessThan(propertyAccess, constant),
                FilterOperator.LessThanOrEqual => Expression.LessThanOrEqual(propertyAccess, constant),
                FilterOperator.NotEquals => Expression.NotEqual(propertyAccess, constant),
                FilterOperator.StartsWith => Expression.Call(propertyAccess, typeof(string).GetMethod("StartsWith", new[] { typeof(string) }), constant),
                FilterOperator.EndsWith => Expression.Call(propertyAccess, typeof(string).GetMethod("EndsWith", new[] { typeof(string) }), constant),
                _ => throw new InvalidOperationException("Unsupported filter operator")
            };

            combined = combined == null ? expr : Expression.AndAlso(combined, expr);
        }

        if (combined != null)
        {
            var lambda = Expression.Lambda<Func<T, bool>>(combined, parameter);
            queryable = queryable.Where(lambda);
        }

        return queryable;
    }

    public static IQueryable<TEntity> OrderQueryableDynamicly<TEntity>(this IQueryable<TEntity> @this, IEnumerable<(string, string)> sortTuples)
    {
        if (sortTuples == null)
            return @this;
        sortTuples = sortTuples.ToList();
        var result = CreateExpression(sortTuples.First().Item1, sortTuples.First().Item2, "orderDirection", @this);
        @this = @this.Provider.CreateQuery<TEntity>(result);
        for (int i = 1; i < sortTuples.Count(); i++)
        {
            var resultExp = CreateExpression(sortTuples.First().Item1, sortTuples.First().Item2, "then", @this);
            @this = @this.Provider.CreateQuery<TEntity>(resultExp);
        }
        return @this;
    }

    private static MethodCallExpression CreateExpression<TEntity>(string prop, string orderDirection, string orderByOrThenBy, IQueryable<TEntity> queryable)
    {
        var expressionParam = Expression.Parameter(typeof(TEntity), "x");
        Expression expressionProperty;
        if (prop.Contains('.'))
        {
            var props = prop.Split('.');
            expressionProperty = Expression.Property(expressionParam, props[0]);
            for (var i = 1; i < props.Length; i++)
            {
                expressionProperty = Expression.Property(expressionProperty, props[i]);
            }
        }
        else
            expressionProperty = Expression.Property(expressionParam, prop);
        var expression = Expression.Lambda(expressionProperty, expressionParam);
        string method = string.Empty;
        if (orderByOrThenBy == "orderDirection")
            method += "OrderBy";
        else
            method += "ThenBy";
        if (!string.Equals(orderDirection, "asc", StringComparison.OrdinalIgnoreCase))
            method += "Descending";
        return Expression.Call(typeof(Queryable), method, new[] { typeof(TEntity), expressionProperty.Type }, queryable.Expression, Expression.Quote(expression));
    }

    public static async Task<Pageable<TEntity>> ToPage<TEntity>(this IQueryable<TEntity> @this, int pageIndex, int pageSize)
    {
        var items = await @this.ToPageableQuery(pageIndex, pageSize).ToListAsync().ConfigureAwait(false);
        int totalItemCount = await @this.CountAsync().ConfigureAwait(false);
        return new Pageable<TEntity>(items, pageIndex, pageSize, totalItemCount);
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

    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, bool>> predicate)
        => condition ? query.Where(predicate) : query;


}