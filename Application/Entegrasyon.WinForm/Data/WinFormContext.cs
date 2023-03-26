using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.WinForm.Data;

public class WinFormContext:DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(@"Data Source=EntegrasyonDb.db;Version=3;");
        base.OnConfiguring(optionsBuilder);
    }

}