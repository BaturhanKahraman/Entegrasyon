using Microsoft.AspNetCore.Components;
using AppCategoryAttribute = Entegrasyon.Entity.Categories.CategoryAttribute;

namespace Entegrasyon.Blazor.Features.Attributes;

public partial class AttributeDetailPanel
{
    [Parameter] public AppCategoryAttribute? SelectedAttribute { get; set; }
}
