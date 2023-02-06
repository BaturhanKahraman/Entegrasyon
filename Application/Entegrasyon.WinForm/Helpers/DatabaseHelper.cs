using Entegrasyon.WinForm.Data;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.WinForm.Helpers;

public class DatabaseHelper
{
    public static async Task CreateDatabase()
    {
        await using var ctx = new WinFormContext();
        var migrations = await ctx.Database.GetPendingMigrationsAsync();
        if (migrations.Any())
        {
            await ctx.Database.MigrateAsync();
        }
    }
}