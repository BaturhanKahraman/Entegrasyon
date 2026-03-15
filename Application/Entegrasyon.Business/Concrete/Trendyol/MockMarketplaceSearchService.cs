using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Concrete.Trendyol;

public sealed class MockMarketplaceSearchService : IMarketplaceSearchService
{
    private const int MaxResults = 20;

    private static readonly List<MarketplaceCategorySearchResult> MockCategories =
    [
        new(1001, "Elektronik", "Elektronik"),
        new(1002, "Bilgisayar", "Elektronik > Bilgisayar"),
        new(1003, "Dizüstü Bilgisayar", "Elektronik > Bilgisayar > Dizüstü Bilgisayar"),
        new(1004, "Masaüstü Bilgisayar", "Elektronik > Bilgisayar > Masaüstü Bilgisayar"),
        new(1005, "Tablet", "Elektronik > Bilgisayar > Tablet"),
        new(1006, "Telefon", "Elektronik > Telefon"),
        new(1007, "Akıllı Telefon", "Elektronik > Telefon > Akıllı Telefon"),
        new(1008, "Telefon Aksesuarları", "Elektronik > Telefon > Aksesuarlar"),
        new(1009, "Kılıf", "Elektronik > Telefon > Aksesuarlar > Kılıf"),
        new(1010, "Şarj Cihazı", "Elektronik > Telefon > Aksesuarlar > Şarj Cihazı"),
        new(2001, "Giyim", "Giyim"),
        new(2002, "Erkek Giyim", "Giyim > Erkek"),
        new(2003, "Kadın Giyim", "Giyim > Kadın"),
        new(2004, "T-Shirt", "Giyim > Erkek > T-Shirt"),
        new(2005, "Pantolon", "Giyim > Erkek > Pantolon"),
        new(2006, "Elbise", "Giyim > Kadın > Elbise"),
        new(2007, "Etek", "Giyim > Kadın > Etek"),
        new(2008, "Çocuk Giyim", "Giyim > Çocuk"),
        new(3001, "Ev & Yaşam", "Ev & Yaşam"),
        new(3002, "Mobilya", "Ev & Yaşam > Mobilya"),
        new(3003, "Koltuk", "Ev & Yaşam > Mobilya > Koltuk"),
        new(3004, "Masa", "Ev & Yaşam > Mobilya > Masa"),
        new(3005, "Mutfak", "Ev & Yaşam > Mutfak"),
        new(3006, "Tencere", "Ev & Yaşam > Mutfak > Tencere"),
        new(4001, "Spor & Outdoor", "Spor & Outdoor"),
        new(4002, "Fitness", "Spor & Outdoor > Fitness"),
        new(4003, "Kamp", "Spor & Outdoor > Kamp"),
        new(4004, "Bisiklet", "Spor & Outdoor > Bisiklet"),
        new(5001, "Kozmetik", "Kozmetik"),
        new(5002, "Parfüm", "Kozmetik > Parfüm"),
    ];

    private static readonly List<MarketplaceBrandSearchResult> MockBrands =
    [
        new(101, "Apple"),
        new(102, "Samsung"),
        new(103, "Xiaomi"),
        new(104, "Huawei"),
        new(105, "Sony"),
        new(106, "LG"),
        new(107, "Asus"),
        new(108, "Lenovo"),
        new(109, "HP"),
        new(110, "Dell"),
        new(111, "Nike"),
        new(112, "Adidas"),
        new(113, "Puma"),
        new(114, "Under Armour"),
        new(115, "New Balance"),
        new(116, "Zara"),
        new(117, "H&M"),
        new(118, "Mango"),
        new(119, "LC Waikiki"),
        new(120, "DeFacto"),
        new(121, "Ikea"),
        new(122, "Bellona"),
        new(123, "İstikbal"),
        new(124, "Karaca"),
        new(125, "Tefal"),
        new(126, "Bosch"),
        new(127, "Philips"),
        new(128, "Dyson"),
        new(129, "Loreal"),
        new(130, "Nivea"),
    ];

    private static readonly List<MarketplaceAttributeSearchResult> MockAttributes =
    [
        new(501, "Renk"),
        new(502, "Beden"),
        new(503, "Malzeme"),
        new(504, "Cinsiyet"),
        new(505, "Garanti Süresi"),
        new(506, "Menşei"),
        new(507, "Ağırlık"),
        new(508, "Genişlik"),
        new(509, "Yükseklik"),
        new(510, "Derinlik"),
        new(511, "Kapasite"),
        new(512, "Güç"),
        new(513, "Voltaj"),
        new(514, "Pil Ömrü"),
        new(515, "Ekran Boyutu"),
        new(516, "Çözünürlük"),
        new(517, "İşlemci"),
        new(518, "RAM"),
        new(519, "Depolama"),
        new(520, "İşletim Sistemi"),
        new(521, "Bağlantı"),
        new(522, "Kumaş Tipi"),
        new(523, "Yıkama Talimatı"),
        new(524, "Kalıp"),
        new(525, "Kol Tipi"),
        new(526, "Yaka Tipi"),
        new(527, "Desen"),
        new(528, "Sezon"),
        new(529, "Paket İçeriği"),
        new(530, "Ürün Kodu"),
    ];

    public Task<IDataResult<List<MarketplaceCategorySearchResult>>> SearchCategoriesAsync(
        int marketPlaceId, string query, CancellationToken ct = default)
    {
        var results = Filter(MockCategories, query, x => x.Name, x => x.FullPath);
        return Task.FromResult<IDataResult<List<MarketplaceCategorySearchResult>>>(
            new SuccessDataResult<List<MarketplaceCategorySearchResult>>(results));
    }

    public Task<IDataResult<List<MarketplaceBrandSearchResult>>> SearchBrandsAsync(
        int marketPlaceId, string query, CancellationToken ct = default)
    {
        var results = Filter(MockBrands, query, x => x.Name);
        return Task.FromResult<IDataResult<List<MarketplaceBrandSearchResult>>>(
            new SuccessDataResult<List<MarketplaceBrandSearchResult>>(results));
    }

    public Task<IDataResult<List<MarketplaceAttributeSearchResult>>> SearchAttributesAsync(
        int marketPlaceId, string query, CancellationToken ct = default)
    {
        var results = Filter(MockAttributes, query, x => x.Name);
        return Task.FromResult<IDataResult<List<MarketplaceAttributeSearchResult>>>(
            new SuccessDataResult<List<MarketplaceAttributeSearchResult>>(results));
    }

    private static List<T> Filter<T>(List<T> source, string query, params Func<T, string?>[] selectors)
    {
        if (string.IsNullOrWhiteSpace(query))
            return source.Take(MaxResults).ToList();

        return source
            .Where(item => selectors.Any(s =>
                s(item)?.Contains(query, StringComparison.OrdinalIgnoreCase) == true))
            .Take(MaxResults)
            .ToList();
    }
}
