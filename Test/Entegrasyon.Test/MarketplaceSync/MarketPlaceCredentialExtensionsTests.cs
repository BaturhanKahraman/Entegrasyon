using Entegrasyon.Entity;
using FluentAssertions;

namespace Entegrasyon.UnitTest.MarketplaceSync;

/// <summary>
/// MarketPlace.IsCredentialComplete() saf-fonksiyon testleri.
/// Credential tespit kuralı (spec 2026-06-11-marketplace-sync-credential-disabled):
///  - Trendyol (Id=1): ApiKey + ApiSecret + SellerId
///  - Amazon/Pazarama OAuth2 (Id=4/5): TokenUrl + RefreshToken
///  - Diğer: ApiKey + ApiSecret
/// </summary>
public class MarketPlaceCredentialExtensionsTests
{
    [Fact]
    public void Trendyol_TumAnahtarlarDolu_True()
    {
        var mp = new MarketPlace
        {
            Id = 1,
            Name = "Trendyol",
            ApiKey = "key",
            ApiSecret = "secret",
            SellerId = "12345"
        };

        mp.IsCredentialComplete().Should().BeTrue();
    }

    [Fact]
    public void Trendyol_SellerIdBos_False()
    {
        var mp = new MarketPlace
        {
            Id = 1,
            Name = "Trendyol",
            ApiKey = "key",
            ApiSecret = "secret",
            SellerId = ""
        };

        mp.IsCredentialComplete().Should().BeFalse();
    }

    [Theory]
    [InlineData(4)] // Amazon
    [InlineData(5)] // Pazarama
    public void Oauth_TokenUrlVeRefreshTokenDolu_True(int id)
    {
        var mp = new MarketPlace
        {
            Id = id,
            Name = "OAuth MP",
            TokenUrl = "https://token",
            RefreshToken = "refresh"
        };

        mp.IsCredentialComplete().Should().BeTrue();
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    public void Oauth_RefreshTokenBos_False(int id)
    {
        var mp = new MarketPlace
        {
            Id = id,
            Name = "OAuth MP",
            TokenUrl = "https://token",
            RefreshToken = null
        };

        mp.IsCredentialComplete().Should().BeFalse();
    }

    [Fact]
    public void DigerPazaryeri_ApiKeyVeApiSecretDolu_True()
    {
        var mp = new MarketPlace
        {
            Id = 2,
            Name = "Hepsiburada",
            ApiKey = "key",
            ApiSecret = "secret"
        };

        mp.IsCredentialComplete().Should().BeTrue();
    }

    [Fact]
    public void DigerPazaryeri_ApiSecretBos_False()
    {
        var mp = new MarketPlace
        {
            Id = 2,
            Name = "Hepsiburada",
            ApiKey = "key",
            ApiSecret = null
        };

        mp.IsCredentialComplete().Should().BeFalse();
    }

    [Fact]
    public void Trendyol_ApiSecretBos_SellerIdDolu_False()
    {
        // ApiKey+SellerId dolu ama ApiSecret eksik → yine de credential tam değil
        var mp = new MarketPlace
        {
            Id = 1,
            Name = "Trendyol",
            ApiKey = "key",
            ApiSecret = "",
            SellerId = "12345"
        };

        mp.IsCredentialComplete().Should().BeFalse();
    }
}
