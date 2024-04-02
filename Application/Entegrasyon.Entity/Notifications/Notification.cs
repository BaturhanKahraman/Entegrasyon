using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.User;
using Shared.Entity;

namespace Entegrasyon.Entity.Notifications;

public sealed class Notification : BaseEntity
{
    public long Id { get; set; }
    public string Header { get; set; }
    public string Content { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset ReadDate { get; set; }
    public Guid? ApplicationUserId { get; set; }
    public ApplicationUser ApplicationUser { get; set; }

    public int? ApplicationClaimId { get; set; }
    public ApplicationClaim ApplicationClaim { get; set; }
}