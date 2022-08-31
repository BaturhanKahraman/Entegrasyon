using System.ComponentModel.DataAnnotations;
using Shared.Abstract.Entity;

namespace Entegrasyon.Entity.Categories;

public class CategoryAttributeValue : ApplicationEntity
{
    [Required, StringLength(maximumLength: 35,MinimumLength = 3)]
    public string Name { get; set; }
}