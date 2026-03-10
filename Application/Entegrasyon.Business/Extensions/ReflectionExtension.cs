using System.Linq.Expressions;

namespace Entegrasyon.Business.Extensions;

public static class ReflectionExtension
{
    public static string GetClassName(this Type type)
    {
        return type.Name;
    }
    //gets the string of unknown type and returns it as func
    public static Func<T,object> GetPropertyAsFunc<T>(string propertyName)
    {
        var param = Expression.Parameter(typeof(T),"x");
        var property = Expression.Property(param,propertyName);
        var convert = Expression.Convert(property,typeof(object));
        var lambda = Expression.Lambda<Func<T,object>>(convert,param);
        return lambda.Compile();
    }
    //gets the string of unknown type and returns it as Expression
    public static Expression<Func<T,object>> GetPropertyAsExpression<T>(string propertyName)
    {
        var param = Expression.Parameter(typeof(T),"x");
        var property = Expression.Property(param,propertyName);
        var convert = Expression.Convert(property,typeof(object));
        var lambda = Expression.Lambda<Func<T,object>>(convert,param);
        return lambda;
    }
}
