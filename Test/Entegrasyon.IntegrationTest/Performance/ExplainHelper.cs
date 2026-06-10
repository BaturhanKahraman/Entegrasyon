using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Entegrasyon.IntegrationTest.Performance;

/// <summary>
/// Bir sorgunun PostgreSQL EXPLAIN plani uzerinde index kullanimini dogrulamak icin yardimci.
///
/// Testler GERCEKCI olcekte (~50k satir, raw bulk insert) seed edilir; bu olcekte seq scan
/// veya genis partial index taramasi belirgin sekilde pahalidir ve GERCEK planlayici secici
/// esitlik/sirali index'i kendiliginden secer (seqscan hack'ine gerek yok). Plan hedef index
/// adini icermiyorsa (index yok) test RED olur — yani gercek dunya kazanci kanitlanir.
/// </summary>
internal static class ExplainHelper
{
    /// <summary>Verilen SQL icin EXPLAIN (FORMAT TEXT) plan metnini dondurur.</summary>
    public static async Task<string> ExplainAsync(IntegrationDbContext dbContext, string sql)
    {
        var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "EXPLAIN (FORMAT TEXT) " + sql;
        await using var reader = await cmd.ExecuteReaderAsync();

        var lines = new List<string>();
        while (await reader.ReadAsync())
            lines.Add(reader.GetString(0));

        return string.Join('\n', lines);
    }
}
