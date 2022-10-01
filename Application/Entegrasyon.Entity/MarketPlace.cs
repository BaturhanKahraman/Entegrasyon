using System.ComponentModel.DataAnnotations;
using Shared.Entity;

namespace Entegrasyon.Entity;

public class MarketPlace : ApplicationEntity
{
    [Required, StringLength(maximumLength: 50)]
    public string Name { get; set; }

    public string ApiKey { get; set; }
    public string ApiSecret { get; set; }

    public bool IsBasicAuth { get; set; } = false;
    public string BasicAuthUserName { get; set; }
    public string BasicAuthPassword { get; set; }
}