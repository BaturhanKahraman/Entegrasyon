using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Entegrasyon.MVC.Infrastructure.Extensions;

public static class ViewDataExtensions
{
    private const string titleKey = "Title";
    private const string pretitleKey = "PagePretitle";
    private const string subtitleKey = "PageSubtitle";
    private const string activeNavKey = "ActiveNav";
    private const string activeNavGroupKey = "ActiveNavGroup";
    private const string breadcrumbKey = "Breadcrumb";
    private const string bannersKey = "PageBanners";

    /// <summary>
    /// Sayfa başlığını ViewData'ya ekler. Layout sayfasında bu değer kullanılarak sayfa başlığı dinamik olarak gösterilir.
    /// </summary>
    /// <param name="viewData"></param>
    /// <param name="title">Sayfa başlığı olarak gösterilecek metin.</param>
    public static void SetPageTitle(this ViewDataDictionary viewData, string title)
        => viewData[titleKey] = title;

    /// <summary>
    /// Sayfa başlığının üstünde gösterilen küçük üst-başlık (pretitle) metnini ayarlar.
    /// Tabler <c>page-pretitle</c> olarak render edilir (otomatik küçük/uppercase/muted).
    /// </summary>
    /// <param name="viewData"></param>
    /// <param name="pretitle">Üst-başlık metni (ör. "Katalog").</param>
    public static void SetPagePretitle(this ViewDataDictionary viewData, string pretitle)
        => viewData[pretitleKey] = pretitle;

    /// <summary>ViewData'dan sayfa üst-başlığını (pretitle) alır; ayarlanmamışsa boş string döner.</summary>
    public static string GetPagePretitle(this ViewDataDictionary viewData)
        => viewData[pretitleKey] as string ?? "";

    /// <summary>
    /// Sayfa başlığının altında gösterilen açıklama (subtitle) metnini ayarlar.
    /// Layout'ta başlığın altında muted satır olarak render edilir.
    /// </summary>
    /// <param name="viewData"></param>
    /// <param name="subtitle">Kısa açıklama metni.</param>
    public static void SetPageSubtitle(this ViewDataDictionary viewData, string subtitle)
        => viewData[subtitleKey] = subtitle;

    /// <summary>ViewData'dan sayfa açıklamasını (subtitle) alır; ayarlanmamışsa boş string döner.</summary>
    public static string GetPageSubtitle(this ViewDataDictionary viewData)
        => viewData[subtitleKey] as string ?? "";

    /// <summary>
    /// Aktif navigasyon öğesini ViewData'ya ekler. Layout sayfasında bu değer kullanılarak aktif navigasyon öğesi dinamik olarak gösterilir.
    /// </summary>
    /// <param name="viewData"></param>
    /// <param name="nav">Aktif navigasyon öğesi olarak gösterilecek metin.</param>
    public static void SetActiveNav(this ViewDataDictionary viewData, string nav)
        => viewData[activeNavKey] = nav;

    /// <summary>
    /// Aktif navigasyon grubunu ViewData'ya ekler. Layout sayfasında bu değer kullanılarak aktif navigasyon grubu dinamik olarak gösterilir.
    /// </summary>
    /// <param name="viewData"></param>
    /// <param name="group">Aktif navigasyon grubu olarak gösterilecek metin.</param>

    public static void SetActiveNavGroup(this ViewDataDictionary viewData, string group)
        => viewData[activeNavGroupKey] = group;

    /// <summary>
    /// Breadcrumb (sayfa yolunu) ViewData'ya ekler. Layout sayfasında bu değer kullanılarak breadcrumb dinamik olarak gösterilir.
    /// </summary>
    /// <param name="viewData"></param>
    /// <param name="crumbs">Breadcrumb öğeleri olarak gösterilecek metin ve URL çiftleri.</param>

    public static void SetBreadcrumb(this ViewDataDictionary viewData,
        params (string Text, string? Url)[] crumbs)
        => viewData[breadcrumbKey] = crumbs;
    /// <summary>
    /// ViewData'dan sayfa başlığını alır. Eğer başlık ayarlanmamışsa varsayılan olarak "Entegrasyon" döner.
    /// </summary>
    /// <param name="viewData"></param>
    /// <returns>Sayfa başlığı olarak gösterilecek metin.</returns>
    public static string GetPageTitle(this ViewDataDictionary viewData)
        => viewData[titleKey] as string ?? "Entegrasyon";
    /// <summary>
    /// ViewData'dan aktif navigasyon öğesini alır. Eğer aktif navigasyon öğesi ayarlanmamışsa varsayılan olarak boş string döner.
    /// </summary>
    /// <param name="viewData"></param>
    /// <returns>Aktif navigasyon öğesi olarak gösterilecek metin.</returns>
    public static string GetActiveNav(this ViewDataDictionary viewData)
        => viewData[activeNavKey] as string ?? "";

    /// <summary>
    /// ViewData'dan aktif navigasyon grubunu alır. Eğer aktif navigasyon grubu ayarlanmamışsa varsayılan olarak boş string döner.
    /// </summary>
    /// <param name="viewData"></param>
    /// <returns>Aktif navigasyon grubu olarak gösterilecek metin.</returns>
    public static string GetActiveNavGroup(this ViewDataDictionary viewData)
        => viewData[activeNavGroupKey] as string ?? "";
    /// <summary>
    /// ViewData'dan breadcrumb öğelerini alır. Eğer breadcrumb ayarlanmamışsa varsayılan olarak boş bir dizi döner.
    /// </summary>
    /// <param name="viewData"></param>
    /// <returns>Breadcrumb öğeleri olarak gösterilecek metin ve URL çiftleri.</returns>
    public static (string Text, string? Url)[] GetBreadcrumb(this ViewDataDictionary viewData)
        => viewData[breadcrumbKey] as (string, string?)[] ?? [];

    /// <summary>Sayfa ustunde kalici banner gosterir. type: info | success | warning | danger</summary>
    /// <param name="viewData"></param>
    /// <param name="type">Banner türü.</param>
    /// <param name="message">Banner mesajı.</param>
    /// <param name="id">Banner ID'si (isteğe bağlı).</param>
    public static void AddBanner(this ViewDataDictionary viewData, string type, string message, string? id = null)
    {
        var banners = viewData.GetBanners();
        banners.Add(new PageBanner(type, message, id));
        viewData[bannersKey] = banners;
    }
    /// <summary>
    /// ViewData'dan sayfa banner'larını alır. Eğer banner'lar ayarlanmamışsa varsayılan olarak boş bir liste döner.
    /// </summary>
    /// <param name="viewData"></param>
    /// <returns>Sayfa banner'ları olarak gösterilecek liste.</returns>
    public static List<PageBanner> GetBanners(this ViewDataDictionary viewData)
        => viewData[bannersKey] as List<PageBanner> ?? [];
}
/// <summary>
/// Sayfa banner'ı için kullanılan kayıt türü. Type: info | success | warning | danger, Message: Banner mesajı, Id: Banner ID'si (isteğe bağlı).
/// </summary>
/// <param name="Type">Banner türü.</param>
/// <param name="Message">Banner mesajı.</param>
/// <param name="Id">Banner ID'si (isteğe bağlı).</param>
public record PageBanner(string Type, string Message, string? Id = null);
