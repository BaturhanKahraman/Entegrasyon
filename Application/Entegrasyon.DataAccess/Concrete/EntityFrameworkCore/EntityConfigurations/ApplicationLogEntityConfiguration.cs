
using Entegrasyon.Entity.Logs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations
{
    public class ApplicationLogEntityConfiguration : IEntityTypeConfiguration<ApplicationLog>
    {
        public void Configure(EntityTypeBuilder<ApplicationLog> builder)
        {
           
        }
    }
}
