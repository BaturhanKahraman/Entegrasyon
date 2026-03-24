using Entegrasyon.Entity.Dtos.Shipping;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.Shipping;

public partial class CargoSummaryCards : ComponentBase
{
    [Parameter]
    public CargoSummaryDto? Summary { get; set; }

    [Parameter]
    public bool IsLoading { get; set; }
}
