namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontProductQuestion : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Guid ProductId { get; set; }
    public int CustomerId { get; set; }
    public string QuestionText { get; set; } = null!;
    public string? AnswerText { get; set; }
    public DateTimeOffset? AnsweredAt { get; set; }
    public bool IsPublished { get; set; }
    public int HelpfulCount { get; set; }
}
