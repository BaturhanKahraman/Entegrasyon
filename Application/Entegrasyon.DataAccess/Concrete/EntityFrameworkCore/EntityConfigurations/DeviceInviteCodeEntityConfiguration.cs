using Entegrasyon.Entity.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class DeviceInviteCodeEntityConfiguration : IEntityTypeConfiguration<DeviceInviteCode>
{
    public void Configure(EntityTypeBuilder<DeviceInviteCode> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.TenantId);
    }
}
