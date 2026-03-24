using Entegrasyon.Entity.Dtos.Templates;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.MatchedEntityImport;

public partial class MatchedEntityPackageCard
{
    [Parameter, EditorRequired]
    public MatchedEntityPackageDto Package { get; set; } = null!;

    [Parameter]
    public EventCallback<MatchedEntityPackageDto> OnViewDetail { get; set; }

    [Parameter]
    public EventCallback<MatchedEntityPackageDto> OnImport { get; set; }
}
