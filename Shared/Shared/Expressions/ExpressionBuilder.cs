using System.Linq.Expressions;
using Shared.Extensions;

namespace Shared.Expressions;

public class ExpressionBuilder<T>
where T:class
{
    private Expression<Func<T, bool>> expression;
    
    public ExpressionBuilder<T> AddAnd(Expression<Func<T, bool>> expr,bool condition)
    {
        expression = expression.AndIf(condition, expr);
        return this;
    }
    public ExpressionBuilder<T> AddOr(Expression<Func<T,bool>> expr,bool condition)
    {
        expression = expression.OrIf(condition,expr);
        return this;
    }

    public Expression<Func<T, bool>> Build() => expression;

}