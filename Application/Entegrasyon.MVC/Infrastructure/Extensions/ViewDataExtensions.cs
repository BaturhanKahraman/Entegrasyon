using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Entegrasyon.MVC.Infrastructure.Extensions;

public static class ViewDataExtensions
{
    private const string TitleKey = "Title";
    private const string ActiveNavKey = "ActiveNav";
    private const string ActiveNavGroupKey = "ActiveNavGroup";
    private const string BreadcrumbKey = "Breadcrumb";
    private const string BannersKey = "PageBanners";

    public static void SetPageTitle(this ViewDataDictionary viewData, string title)
        => viewData[TitleKey] = title;

    public static void SetActiveNav(this ViewDataDictionary viewData, string nav)
        => viewData[ActiveNavKey] = nav;

    public static void SetActiveNavGroup(this ViewDataDictionary viewData, string group)
        => viewData[ActiveNavGroupKey] = group;

    public static void SetBreadcrumb(this ViewDataDictionary viewData,
        params (string Text, string? Url)[] crumbs)
        => viewData[BreadcrumbKey] = crumbs;

    public static string GetPageTitle(this ViewDataDictionary viewData)
        => viewData[TitleKey] as string ?? "Entegrasyon";

    public static string GetActiveNav(this ViewDataDictionary viewData)
        => viewData[ActiveNavKey] as string ?? "";

    public static string GetActiveNavGroup(this ViewDataDictionary viewData)
        => viewData[ActiveNavGroupKey] as string ?? "";

    public static (string Text, string? Url)[] GetBreadcrumb(this ViewDataDictionary viewData)
        => viewData[BreadcrumbKey] as (string, string?)[] ?? [];

    /// <summary>Sayfa ustunde kalici banner gosterir. type: info | success | warning | danger</summary>
    public static void AddBanner(this ViewDataDictionary viewData, string type, string message, string? id = null)
    {
        var banners = viewData.GetBanners();
        banners.Add(new PageBanner(type, message, id));
        viewData[BannersKey] = banners;
    }

    public static List<PageBanner> GetBanners(this ViewDataDictionary viewData)
        => viewData[BannersKey] as List<PageBanner> ?? [];
}

public record PageBanner(string Type, string Message, string? Id = null);
