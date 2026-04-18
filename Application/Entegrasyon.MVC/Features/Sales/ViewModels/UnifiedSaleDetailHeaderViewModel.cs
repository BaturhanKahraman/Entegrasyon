namespace Entegrasyon.MVC.Features.Sales.ViewModels;

public sealed class UnifiedSaleDetailHeaderViewModel
{
    public string Number { get; init; } = "";
    public string SourceLabel { get; init; } = "";
    public string SourceBadgeClass { get; init; } = "bg-blue-lt text-blue";
    public string StatusLabel { get; init; } = "";
    public string StatusBadgeClass { get; init; } = "bg-green-lt text-green";
    public DateTimeOffset SaleDate { get; init; }
    public string? Subtitle { get; init; }
    public List<ActionGroup> ActionGroups { get; init; } = [];

    public sealed class ActionGroup
    {
        public string? Header { get; init; }
        public List<ActionItem> Actions { get; init; } = [];
    }

    public sealed class ActionItem
    {
        public string Label { get; init; } = "";
        public string Icon { get; init; } = "ti-circle";
        public string? Href { get; init; }
        public string? HxPost { get; init; }
    }
}
