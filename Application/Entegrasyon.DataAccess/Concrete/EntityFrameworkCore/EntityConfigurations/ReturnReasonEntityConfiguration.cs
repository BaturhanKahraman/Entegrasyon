using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class ReturnReasonEntityConfiguration : IEntityTypeConfiguration<ReturnReason>
{
    public void Configure(EntityTypeBuilder<ReturnReason> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.IsActive);

        builder.HasData(
            new ReturnReason { Id = 1, Code = "SIZE_MISMATCH",        Name = "Beden uymuyor",            SortOrder = 10, IsActive = true, IsSystem = true, CreatedAt = DateTimeOffset.UnixEpoch, UpdatedAt = DateTimeOffset.UnixEpoch },
            new ReturnReason { Id = 2, Code = "DEFECTIVE",            Name = "Hatalı ürün",              SortOrder = 20, IsActive = true, IsSystem = true, CreatedAt = DateTimeOffset.UnixEpoch, UpdatedAt = DateTimeOffset.UnixEpoch },
            new ReturnReason { Id = 3, Code = "DAMAGED_IN_SHIPPING",  Name = "Kargoda hasar gördü",      SortOrder = 30, IsActive = true, IsSystem = true, CreatedAt = DateTimeOffset.UnixEpoch, UpdatedAt = DateTimeOffset.UnixEpoch },
            new ReturnReason { Id = 4, Code = "CUSTOMER_CHANGED_MIND",Name = "Müşteri vazgeçti",         SortOrder = 40, IsActive = true, IsSystem = true, CreatedAt = DateTimeOffset.UnixEpoch, UpdatedAt = DateTimeOffset.UnixEpoch },
            new ReturnReason { Id = 5, Code = "WRONG_ITEM_SENT",      Name = "Yanlış ürün gönderildi",   SortOrder = 50, IsActive = true, IsSystem = true, CreatedAt = DateTimeOffset.UnixEpoch, UpdatedAt = DateTimeOffset.UnixEpoch },
            new ReturnReason { Id = 6, Code = "PRICE_DIFFERENCE",     Name = "Fiyat farkı",              SortOrder = 60, IsActive = true, IsSystem = true, CreatedAt = DateTimeOffset.UnixEpoch, UpdatedAt = DateTimeOffset.UnixEpoch },
            new ReturnReason { Id = 7, Code = "OTHER",                Name = "Diğer",                    SortOrder = 99, IsActive = true, IsSystem = true, CreatedAt = DateTimeOffset.UnixEpoch, UpdatedAt = DateTimeOffset.UnixEpoch }
        );
    }
}
