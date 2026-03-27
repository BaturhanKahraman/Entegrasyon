using System.Linq.Expressions;

namespace Entegrasyon.Business.Extensions;

public static class ExpressionExtension
{
    public static Expression<Func<T,bool>> OrIf<T>(this Expression<Func<T,bool>>? expr1,bool condition,Expression<Func<T,bool>> expr2)
    {
        if(!condition)
            return expr1!;
        return expr1.Or(expr2);
    }
    public static Expression<Func<T,bool>> AndIf<T>(this Expression<Func<T,bool>>? expr1,bool condition,Expression<Func<T,bool>> expr2)
    {
        if (!condition)
            return expr1!;
        return expr1.And(expr2);
    }
    public static Expression<Func<T,bool>> And<T>(this Expression<Func<T,bool>>? expr1,Expression<Func<T,bool>> expr2)
    {
        if(expr1 == null)
            return expr2;
        var invokedExpr = Expression.Invoke(expr2,expr1.Parameters);
        return Expression.Lambda<Func<T,bool>>(Expression.AndAlso(expr1.Body,invokedExpr),expr1.Parameters);
    }

    public static Expression<Func<T,bool>> Or<T>(this Expression<Func<T,bool>>? expr1,Expression<Func<T,bool>> expr2)
    {
        if(expr1 == null)
            return expr2;
        var invokedExpr = Expression.Invoke(expr2,expr1.Parameters);
        return Expression.Lambda<Func<T,bool>>(Expression.OrElse(expr1.Body,invokedExpr),expr1.Parameters);
    }
    public static Expression<Func<T,TResult>> ToExpression<T, TResult>(this Func<T,TResult> func) => x => func(x);
}
