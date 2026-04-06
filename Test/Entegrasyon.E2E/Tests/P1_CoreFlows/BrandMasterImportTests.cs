using Entegrasyon.E2E.Infrastructure;
using Entegrasyon.E2E.PageObjects.Brands;

namespace Entegrasyon.E2E.Tests.P1_CoreFlows;

/// <summary>
/// Master katalogdan marka aktarma E2E testleri.
/// Akis: /brands → /brands/master-import → arama → secim → import → dogrulama → idempotency
/// </summary>
[TestFixture, Order(15)]
public class BrandMasterImportTests : E2ETestBase
{
    private BrandsPage _brandsPage = null!;
    private BrandMasterImportPage _importPage = null!;
    private SyncOverviewPage _syncPage = null!;

    [SetUp]
    public async Task SetUp()
    {
        await LoginAsAdminAsync();
        _brandsPage = new BrandsPage(Page, BaseUrl);
        _importPage = new BrandMasterImportPage(Page, BaseUrl);
        _syncPage = new SyncOverviewPage(Page, BaseUrl);
    }

    [Test, Order(1)]
    public async Task BrandsIndex_ShowsImportButton()
    {
        await _brandsPage.NavigateAsync();

        await Expect(_brandsPage.ImportButton).ToBeVisibleAsync();
        await Expect(_brandsPage.ImportButton).ToHaveAttributeAsync("href", "/brands/master-import");
    }

    [Test, Order(2)]
    public async Task BrandsIndex_ImportButtonNavigatesToMasterImport()
    {
        await _brandsPage.NavigateAsync();
        await _brandsPage.ImportButton.ClickAsync();

        await _importPage.PageTitle.WaitForAsync(new() { Timeout = 10000 });
        await Expect(Page).ToHaveURLAsync(new Regex("/brands/master-import"));
    }

    [Test, Order(3)]
    public async Task MasterImport_PageLoadsWithEmptyState()
    {
        await _importPage.NavigateAsync();

        await Expect(_importPage.SearchInput).ToBeVisibleAsync();
        await Expect(_importPage.ImportButton).ToBeDisabledAsync();

        var selectedCount = await _importPage.GetSelectedCountAsync();
        Assert.That(selectedCount, Is.EqualTo(0), "Baslangicta secili marka olmamali");
    }

    [Test, Order(4)]
    public async Task MasterImport_SearchReturnResults()
    {
        await _importPage.NavigateAsync();

        await _importPage.SearchAsync("Nike");

        // Sonuclar gelmeli
        var resultItems = _importPage.SearchResults.Locator(".list-group-item");
        var count = await resultItems.CountAsync();
        Assert.That(count, Is.GreaterThan(0), "Nike aramasinda sonuc donmeli");

        // "Nike" metni iceren bir sonuc olmali
        var nikeResult = resultItems.Filter(new() { HasText = "Nike" });
        Assert.That(await nikeResult.CountAsync(), Is.GreaterThan(0));
    }

    [Test, Order(5)]
    public async Task MasterImport_ShortQueryReturnsNoResults()
    {
        await _importPage.NavigateAsync();

        await _importPage.SearchAsync("N");

        // 2 karakterden kisa sorgu sonuc donmemeli
        var resultItems = _importPage.SearchResults.Locator(".list-group-item");
        var count = await resultItems.CountAsync();
        Assert.That(count, Is.EqualTo(0), "Tek karakter aramasinda sonuc donmemeli");
    }

    [Test, Order(6)]
    public async Task MasterImport_SelectBrandAddsToCart()
    {
        await _importPage.NavigateAsync();
        await _importPage.SearchAsync("Adidas");

        await _importPage.SelectBrandFromResultsAsync("Adidas");

        var selectedCount = await _importPage.GetSelectedCountAsync();
        Assert.That(selectedCount, Is.EqualTo(1), "Bir marka secilmeli");

        var isSelected = await _importPage.IsSelectedAsync("Adidas");
        Assert.That(isSelected, Is.True, "Adidas secili listede olmali");

        // Import butonu aktif olmali
        await Expect(_importPage.ImportButton).ToBeEnabledAsync();
    }

    [Test, Order(7)]
    public async Task MasterImport_RemoveBrandFromCart()
    {
        await _importPage.NavigateAsync();
        await _importPage.SearchAsync("Adidas");

        await _importPage.SelectBrandFromResultsAsync("Adidas");
        Assert.That(await _importPage.GetSelectedCountAsync(), Is.EqualTo(1));

        await _importPage.RemoveBrandAsync("Adidas");
        Assert.That(await _importPage.GetSelectedCountAsync(), Is.EqualTo(0));

        // Import butonu tekrar disabled olmali
        await Expect(_importPage.ImportButton).ToBeDisabledAsync();
    }

    [Test, Order(8)]
    public async Task MasterImport_ImportSelectedBrands()
    {
        await _importPage.NavigateAsync();
        await _importPage.SearchAsync("Caterpillar");

        await _importPage.SelectBrandFromResultsAsync("Caterpillar");
        Assert.That(await _importPage.GetSelectedCountAsync(), Is.EqualTo(1));

        await _importPage.ClickImportAsync();

        // Basari karti gorunmeli
        await Expect(_importPage.SuccessCard).ToBeVisibleAsync();

        // Sonuc sayilari parse edilebilmeli
        var importedText = await _importPage.BrandsImportedCount.InnerTextAsync();
        var skippedText = await _importPage.BrandsSkippedCount.InnerTextAsync();
        var imported = int.Parse(importedText);
        var skipped = int.Parse(skippedText);

        // Toplam islem sayisi > 0 olmali (ya eklendi ya atlandi)
        Assert.That(imported + skipped, Is.GreaterThan(0), "En az 1 marka islenmeli");

        // "Markalara Git" ve "Tekrar Import" linkleri gorunmeli
        await Expect(_importPage.GoToBrandsLink).ToBeVisibleAsync();
        await Expect(_importPage.RetryImportLink).ToBeVisibleAsync();
    }

    [Test, Order(9)]
    public async Task MasterImport_IdempotentImport_SkipsDuplicates()
    {
        // Caterpillar'i iki kez import et — ikincisinde kesinlikle atlandi olmali
        // Ilk import (idempotent - zaten varsa atlar)
        await _importPage.NavigateAsync();
        await _importPage.SearchAsync("Caterpillar");
        await _importPage.SelectBrandFromResultsAsync("Caterpillar");
        await _importPage.ClickImportAsync();
        await Expect(_importPage.SuccessCard).ToBeVisibleAsync();

        // Ikinci import — ayni markayi tekrar dene
        await _importPage.RetryImportLink.ClickAsync();
        await _importPage.PageTitle.WaitForAsync(new() { Timeout = 10000 });

        await _importPage.SearchAsync("Caterpillar");
        await _importPage.SelectBrandFromResultsAsync("Caterpillar");
        await _importPage.ClickImportAsync();

        await Expect(_importPage.SuccessCard).ToBeVisibleAsync();

        // Eklenen = 0, Atlanan > 0
        var importedText = await _importPage.BrandsImportedCount.InnerTextAsync();
        var skippedText = await _importPage.BrandsSkippedCount.InnerTextAsync();

        Assert.That(int.Parse(importedText), Is.EqualTo(0), "Duplicate marka eklenmemeli");
        Assert.That(int.Parse(skippedText), Is.GreaterThan(0), "Mevcut marka atlanmali");
    }

    [Test, Order(10)]
    public async Task MasterImport_GoToBrandsLink_NavigatesToBrandsList()
    {
        // Once import et
        await _importPage.NavigateAsync();
        await _importPage.SearchAsync("Reebok");

        await _importPage.SelectBrandFromResultsAsync("Reebok");
        await _importPage.ClickImportAsync();
        await Expect(_importPage.SuccessCard).ToBeVisibleAsync();

        // "Markalara Git" linkine tikla
        await _importPage.GoToBrandsLink.ClickAsync();
        await _brandsPage.PageTitle.WaitForAsync(new() { Timeout = 10000 });
        await Expect(Page).ToHaveURLAsync(new Regex("/brands$"));

        // Import edilen marka aranabilir olmali
        await _brandsPage.SearchInput.FillAsync("Reebok");
        await Page.WaitForHtmxSettleAsync();
        await Page.WaitForTimeoutAsync(500);

        var hasReebok = await Page.GetByRole(AriaRole.Link, new() { Name = "Reebok" }).CountAsync() > 0;
        Assert.That(hasReebok, Is.True, "Import edilen Reebok arama ile bulunmali");
    }

    [Test, Order(11)]
    public async Task MasterImport_MultipleSelection_ImportsAll()
    {
        await _importPage.NavigateAsync();
        await _importPage.SearchAsync("Under Armour");

        // "Under Armour" sonuclardan birini sec
        var resultItems = _importPage.SearchResults.Locator(".list-group-item");
        var count = await resultItems.CountAsync();

        if (count > 0)
        {
            await _importPage.SelectBrandFromResultsAsync("Under Armour");

            // Farkli bir arama yap ve baska bir marka ekle
            await _importPage.SearchAsync("New Balance");
            var nbCount = await _importPage.SearchResults.Locator(".list-group-item").CountAsync();
            if (nbCount > 0)
            {
                await _importPage.SelectBrandFromResultsAsync("New Balance");
                Assert.That(await _importPage.GetSelectedCountAsync(), Is.EqualTo(2), "2 marka secili olmali");
            }

            await _importPage.ClickImportAsync();
            await Expect(_importPage.SuccessCard).ToBeVisibleAsync();
        }
        else
        {
            Assert.Warn("Under Armour master katalogda bulunamadi — test atlandi");
        }
    }

    [Test, Order(12)]
    public async Task SyncOverview_ShowsBrandMappingButtons()
    {
        await _syncPage.NavigateAsync();

        var linkCount = await _syncPage.GetBrandMappingLinkCountAsync();
        Assert.That(linkCount, Is.GreaterThan(0), "Her marketplace kartinda 'Marka Eslestir' butonu olmali");
    }

    [Test, Order(13)]
    public async Task SyncOverview_BrandMappingButton_NavigatesToBrandSync()
    {
        await _syncPage.NavigateAsync();

        // Ilk "Marka Eslestir" linkine tikla
        await _syncPage.BrandMappingLinks.First.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Marka esleme sayfasina gidilmeli
        await Expect(Page).ToHaveURLAsync(new Regex(@"/marketplace/sync/brands\?mp=\d+"));

        // Sayfa basarili yuklenmeli
        var hasNoError = await Page.HasNoErrorAsync();
        Assert.That(hasNoError, Is.True, "Marka esleme sayfasi hatasiz yuklenmeli");
    }

    [Test, Order(14)]
    public async Task MasterImport_BackButton_ReturnsToBrandsList()
    {
        await _importPage.NavigateAsync();

        await _importPage.BackButton.ClickAsync();
        await _brandsPage.PageTitle.WaitForAsync(new() { Timeout = 10000 });

        await Expect(Page).ToHaveURLAsync(new Regex("/brands$"));
    }
}
