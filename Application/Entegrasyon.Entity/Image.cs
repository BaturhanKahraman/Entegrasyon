using System.ComponentModel.DataAnnotations;
using Shared.Abstract.Entity;

namespace Entegrasyon.Entity;

public class Image:ApplicationEntity
{
    [StringLength(450)]
    [DataType("varchar")]
    public string Src { get; set; }
    [MaxLength(100)]
    public string AlternativeText { get; set; }
    [MaxLength(100)]
    public string Description { get; set; }

    public bool IsCoverImage { get; set; } = false;
}