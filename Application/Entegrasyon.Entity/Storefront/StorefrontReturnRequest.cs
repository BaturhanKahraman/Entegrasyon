namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontReturnRequest : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Guid OrderId { get; set; }
    public int CustomerId { get; set; }
    public ReturnStatus Status { get; set; }
    public string Reason { get; set; } = null!;
    public string? Description { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    public decimal? RefundAmount { get; set; }
}

public enum ReturnStatus { Pending, Approved, Rejected, Completed }
