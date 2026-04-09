namespace Entegrasyon.Business.Utility.Constants;

public static class StringConstants
{
    // Marketplace HTTP client isimleri — IHttpClientFactory.CreateClient(name) ile kullanilir.
    // Her named client AddClients() icinde Polly resilience handler ile register edilir.
    public const string TrendyolApi = "TrendyolApi";
    public const string TrendyolEFaturaApi = "TrendyolEFaturaApi";
    public const string HepsiburadaApi = "HepsiburadaApi";
    public const string N11RestApi = "N11Rest";       // mevcut raw string ile uyumluluk
    public const string N11SoapApi = "N11SoapApi";
    public const string PazaramaApi = "PazaramaApi";
    public const string AmazonApi = "AmazonApi";
    public const string PttavmCatalogApi = "PttavmCatalogApi";
    public const string PttavmShipmentApi = "PttavmShipmentApi";
    public const string CiceksepetiApi = "CiceksepetiApi";
    public const string TemuApi = "TemuApi";

    // Kargo clientlari — kapsam disi ama CreateClient() bug fix icin named olmali
    public const string SuratKargoApi = "SuratKargoApi";
    public const string YurticiKargoApi = "YurticiKargo"; // mevcut raw string ile uyumluluk
    public const string ArasKargoApi = "ArasKargo";       // mevcut raw string ile uyumluluk

    // Diger HTTP clientlari
    public const string OllamaApi = "Ollama";
    public const string WebhookApi = "webhook";
}
