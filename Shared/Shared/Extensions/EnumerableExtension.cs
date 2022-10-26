using Shared.Entity;

namespace Shared.Extensions;

public static class EnumerableExtension
{
    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source,bool condition,Func<T,bool> predicate) => 
        condition ? source.Where(predicate) : source;

    public static Pageable<T> ToPageable<T>(this IEnumerable<T> source,int pageIndex,int pageSize,int totalItemCount){
        if(pageIndex<0)
            throw new ArgumentException("Page index must be greater than or equal to 0");
        if(pageSize <= 0)
            throw new ArgumentException("Page size must be greater than 0");
        if(source == null)
            throw new ArgumentNullException(nameof(source));
        int totalPageSize = Convert.ToInt32(Math.Ceiling(totalItemCount / (double)pageSize));
        return new Pageable<T>(source, pageIndex, pageSize, totalItemCount,totalPageSize);
    }
}