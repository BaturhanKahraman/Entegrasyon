using Entegrasyon.Entity.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.EntityConfigurations;

public class ChatMessageReadReceiptEntityConfiguration : IEntityTypeConfiguration<ChatMessageReadReceipt>
{
    public void Configure(EntityTypeBuilder<ChatMessageReadReceipt> builder)
    {
        builder.HasKey(x => new { x.MessageId, x.UserId });
        builder.HasOne(x => x.Message).WithMany().HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.Cascade);
    }
}
