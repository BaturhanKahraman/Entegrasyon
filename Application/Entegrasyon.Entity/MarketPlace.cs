using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity;

public sealed class MarketPlace : BaseEntity
{
    public int Id { get; set; }
    [Required, StringLength(maximumLength: 50)]
    public string Name { get; set; }

    public string ApiKey { get; set; }
    public string ApiSecret { get; set; }

    public bool IsBasicAuth { get; set; } = false;
    public string BasicAuthUserName { get; set; }
    public string BasicAuthPassword { get; set; }
}