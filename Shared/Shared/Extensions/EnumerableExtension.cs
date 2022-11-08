using Shared.Entity;

namespace Shared.Extensions;

public static class EnumerableExtension
{
    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source,bool condition,Func<T,bool> predicate) => 
        condition ? source.Where(predicate) : source;

    public static Pageable<T> ToPage<T>(this IEnumerable<T> @this,int pageIndex,int pageSize){
        if(pageIndex<0)
            throw new ArgumentException("Page index must be greater than or equal to 0");
        if(pageSize <= 0)
            throw new ArgumentException("Page size must be greater than 0");
        if(@this == null)
            throw new ArgumentNullException(nameof(@this));
        int totalItemCount = @this.Count();
        var items = @this.Skip(pageIndex * pageSize).Take(pageSize);
        return new Pageable<T>(items, pageIndex, pageSize, totalItemCount);
    }

}