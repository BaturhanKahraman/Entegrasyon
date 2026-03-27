using Entegrasyon.Entity.Dtos.BulkOperations;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.BulkOperations;

public partial class ExportColumnSelector : ComponentBase
{
    [Parameter] public List<ExportColumnDto>? AvailableColumns { get; set; }
    [Parameter] public List<string>? SelectedColumns { get; set; }
    [Parameter] public EventCallback<List<string>?> SelectedColumnsChanged { get; set; }

    private bool IsSelected(string key)
    {
        // null means all selected
        if (SelectedColumns is null)
            return true;
        return SelectedColumns.Contains(key);
    }

    private async Task ToggleColumn(string key)
    {
        if (AvailableColumns is null) return;

        // If currently null (all selected), initialize with all keys
        var current = SelectedColumns ?? AvailableColumns.Select(c => c.Key).ToList();

        if (current.Contains(key))
        {
            current.Remove(key);
        }
        else
        {
            current.Add(key);
        }

        // If all are selected, set to null (meaning "all")
        var result = current.Count == AvailableColumns.Count ? null : current;
        await SelectedColumnsChanged.InvokeAsync(result);
    }

    private async Task SelectAll()
    {
        await SelectedColumnsChanged.InvokeAsync(null);
    }

    private async Task DeselectAll()
    {
        await SelectedColumnsChanged.InvokeAsync(new List<string>());
    }
}
