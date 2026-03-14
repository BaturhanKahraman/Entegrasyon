using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Entegrasyon.Blazor.Components.Shared;

public partial class NavMenu : ComponentBase, IAsyncDisposable
{
    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    /// <summary>
    /// Maps each menu section ID to its associated routes.
    /// When user navigates to any of these routes, the section expands.
    /// </summary>
    private static readonly Dictionary<string, List<string>> sectionRoutes = new()
    {
        { "admin", ["products", "categories", "attributes", "brands", "sales", "notifications"] },
        { "marketplace", ["marketplace"] },
        { "customers", ["customers", "orders", "invoices"] },
        { "reports", ["reports"] },
        { "users", ["users", "roles"] },
        { "settings", ["settings"] }
    };

    /// <summary>
    /// Tracks which menu sections are currently expanded/collapsed.
    /// </summary>
    private Dictionary<string, bool> ExpandedSections { get; set; } = null!;

    /// <summary>
    /// Tracks whether the nested "Senkronizasyon" sub-group is expanded.
    /// </summary>
    private bool _syncExpanded;

    protected override void OnInitialized()
    {
        ExpandedSections = sectionRoutes.Keys.ToDictionary(k => k, _ => false);

        UpdateMenuStateByLocation();
        NavigationManager.LocationChanged += HandleLocationChanged;
    }

    /// <summary>
    /// Handler for location change events. Updates menu state when user navigates.
    /// Only triggers re-render if the active section actually changed.
    /// </summary>
    private async void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (UpdateMenuStateByLocation())
            await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Updates which menu section should be expanded based on the current URL.
    /// Closes all sections first, then opens the relevant one.
    /// Returns true if the active section changed.
    /// </summary>
    private bool UpdateMenuStateByLocation()
    {
        var previousActive = ExpandedSections.FirstOrDefault(x => x.Value).Key;

        foreach (var key in ExpandedSections.Keys)
            ExpandedSections[key] = false;

        var relativePath = new Uri(NavigationManager.Uri).AbsolutePath.ToLower().TrimStart('/');
        var firstSegment = relativePath.Split('/')[0];

        foreach (var section in sectionRoutes)
        {
            if (section.Value.Any(route => firstSegment == route))
            {
                ExpandedSections[section.Key] = true;
                break;
            }
        }

        _syncExpanded = relativePath.StartsWith("marketplace/sync");

        var newActive = ExpandedSections.FirstOrDefault(x => x.Value).Key;
        return previousActive != newActive;
    }

    public ValueTask DisposeAsync()
    {
        NavigationManager.LocationChanged -= HandleLocationChanged;
        return ValueTask.CompletedTask;
    }
}
