namespace Entegrasyon.Blazor.Utility.Objects;

public sealed record NavigationItem(string Name,string Href,string Icon = "",int Order=0);