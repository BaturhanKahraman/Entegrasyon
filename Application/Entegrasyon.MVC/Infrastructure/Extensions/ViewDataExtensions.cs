using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Entegrasyon.MVC.Infrastructure.Extensions;

public static class ViewDataExtensions
{
    private const string TitleKey = "Title";
    private const string ActiveNavKey = "ActiveNav";
    private const string BreadcrumbKey = "Breadcrumb";

    public static void SetPageTitle(this ViewDataDictionary viewData, string title)
        => viewData[TitleKey] = title;

    public static void SetActiveNav(this ViewDataDictionary viewData, string nav)
        => viewData[ActiveNavKey] = nav;

    public static void SetBreadcrumb(this ViewDataDictionary viewData,
        params (string Text, string? Url)[] crumbs)
        => viewData[BreadcrumbKey] = crumbs;

    public static string GetPageTitle(this ViewDataDictionary viewData)
        => viewData[TitleKey] as string ?? "Entegrasyon";

    public static string GetActiveNav(this ViewDataDictionary viewData)
        => viewData[ActiveNavKey] as string ?? "";

    public static (string Text, string? Url)[] GetBreadcrumb(this ViewDataDictionary viewData)
        => viewData[BreadcrumbKey] as (string, string?)[] ?? [];
}
