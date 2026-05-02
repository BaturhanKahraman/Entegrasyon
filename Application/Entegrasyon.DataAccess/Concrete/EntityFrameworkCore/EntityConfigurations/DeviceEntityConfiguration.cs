using Entegrasyon.Entity.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class DeviceEntityConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.HasIndex(x => x.KeyHash);
        builder.HasIndex(x => x.TenantId);
    }
}
