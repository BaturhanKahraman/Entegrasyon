using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Extensions;

public static class DbContextExtension
{
    public static Task UpdateIgnoringNull<T>(this DbContext ctx,T value) where T : class
    {
        var entityEntry =ctx.Attach(value);
        entityEntry.State = EntityState.Modified;
        var properties = value.GetType().GetProperties();
        foreach (var propertyInfo in properties)
        {
            //if(propertyInfo.GetValue(value)==null)

        }
        return Task.CompletedTask;
    }
}
