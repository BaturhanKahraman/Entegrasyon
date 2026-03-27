using Entegrasyon.Entity.Dtos.BulkOperations;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.BulkOperations;

public partial class ValidationPreviewPanel : ComponentBase
{
    [Parameter] public ImportValidationPreviewDto? Preview { get; set; }

    private const int MaxDisplayErrors = 50;
}
