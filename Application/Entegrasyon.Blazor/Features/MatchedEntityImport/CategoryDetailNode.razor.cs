using Entegrasyon.Entity.Dtos.Templates;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.MatchedEntityImport;

public partial class CategoryDetailNode
{
    [Parameter, EditorRequired]
    public TemplateCategoryDetailDto Category { get; set; } = null!;

    [Parameter]
    public int Depth { get; set; }
}
