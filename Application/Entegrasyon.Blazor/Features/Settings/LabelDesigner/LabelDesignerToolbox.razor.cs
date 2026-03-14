using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.Settings.LabelDesigner;

public partial class LabelDesignerToolbox : ComponentBase
{
    [Parameter] public EventCallback<string> OnAddElement { get; set; }
}
