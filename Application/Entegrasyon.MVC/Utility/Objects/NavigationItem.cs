namespace Entegrasyon.MVC.Utility.Objects;

public sealed record NavigationItem(string Name,string Href,string Icon = "",int Order=0);