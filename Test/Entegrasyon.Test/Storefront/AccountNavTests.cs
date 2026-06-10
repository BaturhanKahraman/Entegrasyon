using Entegrasyon.Storefront.Infrastructure;
using FluentAssertions;

namespace Entegrasyon.Test.Storefront;

// _AccountSidebar "sol menü" aktif öğe belirleme mantığı.
// Bug: aktif key sadece partial'a model olarak elle geçiliyordu; eksik/yanlış key
// gelince menü "dashboard"a (ilk öğe) düşüp asılı kalıyordu. Aktif öğe artık
// istek path'inden türetiliyor — her route kendi öğesini aktif gösterir.
public class AccountNavTests
{
    [Theory]
    [InlineData("/hesabim", "dashboard")]
    [InlineData("/hesabim/", "dashboard")]
    [InlineData("/hesabim/siparislerim", "orders")]
    [InlineData("/hesabim/Sipariş/3f9a/", "orders")]
    [InlineData("/hesabim/iadelerim", "returns")]
    [InlineData("/hesabim/iade-talebi/3f9a", "returns")]
    [InlineData("/hesabim/tekrar-satin-al", "buyagain")]
    [InlineData("/favorilerim", "wishlist")]
    [InlineData("/hesabim/cuzdanim", "wallet")]
    [InlineData("/hesabim/puan-programi", "loyalty")]
    [InlineData("/hesabim/arkadasini-getir", "referral")]
    [InlineData("/hesabim/profil", "profile")]
    [InlineData("/hesabim/e-posta-degistir", "profile")]
    [InlineData("/hesabim/adresler", "addresses")]
    [InlineData("/hesabim/guvenlik", "security")]
    [InlineData("/hesabim/sifre", "password")]
    [InlineData("/hesabim/guvenlik/2fa", "twofa")]
    public void ResolveActiveKey_FromPath_ReturnsMatchingItem(string path, string expected)
    {
        AccountNav.ResolveActiveKey(path, explicitKey: null).Should().Be(expected);
    }

    [Fact]
    public void ResolveActiveKey_TwoFactorPath_IsTwofa_NotSecurity()
    {
        // /hesabim/guvenlik/2fa, /hesabim/guvenlik prefix'iyle çakışmamalı (en uzun eşleşme kazanır).
        AccountNav.ResolveActiveKey("/hesabim/guvenlik/2fa", explicitKey: null).Should().Be("twofa");
    }

    [Fact]
    public void ResolveActiveKey_RootHesabim_IsDashboard_NotPrefixMatchAll()
    {
        // /hesabim TAM eşleşmedir; alt sayfalar onu "dashboard" sanmamalı.
        AccountNav.ResolveActiveKey("/hesabim/profil", explicitKey: null).Should().Be("profile");
    }

    [Fact]
    public void ResolveActiveKey_IsCaseInsensitive()
    {
        AccountNav.ResolveActiveKey("/Hesabim/Siparislerim", explicitKey: null).Should().Be("orders");
    }

    [Fact]
    public void ResolveActiveKey_ExplicitKeyWins_WhenProvided()
    {
        // Açıkça verilen key path'ten önce gelir (geriye dönük uyumluluk / özel durumlar).
        AccountNav.ResolveActiveKey("/hesabim/siparislerim", explicitKey: "returns").Should().Be("returns");
    }

    [Fact]
    public void ResolveActiveKey_UnknownPath_FallsBackToDashboard()
    {
        AccountNav.ResolveActiveKey("/bilinmeyen-sayfa", explicitKey: null).Should().Be("dashboard");
    }

    [Fact]
    public void ResolveActiveKey_NullPath_FallsBackToDashboard()
    {
        AccountNav.ResolveActiveKey(null, explicitKey: null).Should().Be("dashboard");
    }
}
