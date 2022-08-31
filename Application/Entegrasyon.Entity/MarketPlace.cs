using System.ComponentModel.DataAnnotations;
using Shared.Abstract.Entity;

namespace Entegrasyon.Entity;

public class MarketPlace : ApplicationEntity
{
    [Required, StringLength(maximumLength: 50,MinimumLength = 2)]
    public string Name { get; set; }
}