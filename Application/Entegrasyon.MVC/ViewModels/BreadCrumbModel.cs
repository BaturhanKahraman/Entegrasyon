namespace Entegrasyon.MVC.ViewModels;
public sealed record BreadcrumbModel(string Name,string Url,BreadcrumbUsageType UsageType);

public enum BreadcrumbUsageType
{
    Controller, Action
}