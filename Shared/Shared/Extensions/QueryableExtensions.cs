using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Shared.Entity;
using System.ComponentModel;
using System.Reflection;
using System.Linq.Dynamic.Core;
namespace Shared.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplyGlobalSearch<T>(
        this IQueryable<T> query,
        string? searchTerm,
        params string[] searchColumns)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchColumns == null || searchColumns.Length == 0)
            return query;

        var searchExpression = string.Join(" || ", searchColumns.Select(col => $"{col}.Contains(@0)"));

        return query.Where(searchExpression, searchTerm);
    }

    public static async Task<Pageable<TResult>> ToPageableAsync<TResult>(
        this IQueryable<TResult> query,
        PaginatedRequest request,
        CancellationToken cancellationToken = default)
    {
        int totalItemCount = await query.CountAsync(cancellationToken);

        if (totalItemCount == 0)
            return new Pageable<TResult>(Array.Empty<TResult>(), request.PageIndex, request.PageSize, 0);

        if (request.OrderBy.Any())
        {
            string orderByString = string.Join(", ", request.OrderBy);
            query = query.OrderBy(orderByString);
        }

        var items = await query
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new Pageable<TResult>(items, request.PageIndex, request.PageSize, totalItemCount);
    }



}
