using Entegrasyon.Entity;
using Entegrasyon.MVC.Features.IntegrationHealth.ViewModels;
using FluentAssertions;

namespace Entegrasyon.UnitTest.IntegrationHealth;

/// <summary>
/// IntegrationHealthMapper.BuildMarketplaceRows — Entegrasyon Sağlığı sayfasının per-marketplace
/// credential satırları (#81 ile tutarlı). Silinmiş pazaryerleri hariç; credential durumu
/// MarketPlace.IsCredentialComplete() tek kaynağından.
/// </summary>
public class IntegrationHealthMapperTests
{
    [Fact]
    public void BuildMarketplaceRows_CredentialDurumunuDogruYansitir()
    {
        var marketplaces = new List<MarketPlace>
        {
            new() { Id = 1, Name = "Trendyol", ApiKey = "k", ApiSecret = "s", SellerId = "123" }, // tam
            new() { Id = 2, Name = "Hepsiburada", ApiKey = "k", ApiSecret = null },                // eksik
        };

        var rows = IntegrationHealthMapper.BuildMarketplaceRows(marketplaces);

        rows.Should().HaveCount(2);
        rows.Single(r => r.MarketPlaceId == 1).HasCredentials.Should().BeTrue();
        rows.Single(r => r.MarketPlaceId == 2).HasCredentials.Should().BeFalse();
    }

    [Fact]
    public void BuildMarketplaceRows_SilinmisPazaryeriniHaricTutar()
    {
        var marketplaces = new List<MarketPlace>
        {
            new() { Id = 1, Name = "Aktif", ApiKey = "k", ApiSecret = "s" },
            new() { Id = 2, Name = "Silinmiş", ApiKey = "k", ApiSecret = "s", IsDeleted = true },
        };

        var rows = IntegrationHealthMapper.BuildMarketplaceRows(marketplaces);

        rows.Should().ContainSingle(r => r.MarketPlaceId == 1);
        rows.Should().NotContain(r => r.MarketPlaceId == 2);
    }
}
