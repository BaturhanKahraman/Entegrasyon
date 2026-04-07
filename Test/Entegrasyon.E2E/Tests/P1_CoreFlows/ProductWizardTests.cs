using System.Text.RegularExpressions;
using Entegrasyon.E2E.Infrastructure;

namespace Entegrasyon.E2E.Tests.P1_CoreFlows;

/// <summary>
/// Urun ekleme wizard'i (7 adimli) E2E testleri.
/// Akis: Step 1 (temel bilgi) → Step 2 (ozellikler) → Step 3 (varyantlar)
///       → Step 4 (gorseller) → Step 5 (onay) → Step 6 (yayinla) → Step 7 (basari)
/// </summary>
[TestFixture, Order(50)]
public class ProductWizardTests : E2ETestBase
{
    [SetUp]
    public async Task SetUp()
    {
        await LoginAsAdminAsync();
    }

    [Test, Order(1)]
    public async Task FullWizardFlow_CreatesProductWithVariants()
    {
        // Step 1: Temel bilgiler
        await Page.GotoAsync($"{BaseUrl}/products/add");
        await Expect(Page.Locator("[data-wizard-step='1']")).ToBeVisibleAsync();

        // Baslik gir
        await Page.FillAsync("input[name='Title']", "E2E Test Urun " + DateTime.Now.Ticks);

        // Marka sec (Tom Select — JS ile)
        await Page.EvaluateAsync(@"() => {
            var sel = document.getElementById('brand-select');
            if (sel && sel.tomselect) {
                var opt = Object.values(sel.tomselect.options).find(o => o.text === 'Nike');
                if (opt) sel.tomselect.setValue(opt.value);
            }
        }");
        // Brand secildigini dogrula
        var brandValue = await Page.EvaluateAsync<string>("() => document.getElementById('brand-select')?.value || ''");
        Assert.That(brandValue, Is.Not.Empty, "Brand ID secilmeli");

        // Kategori sec (Tom Select — JS ile)
        await Page.EvaluateAsync(@"() => {
            var sel = document.getElementById('category-select');
            if (sel && sel.tomselect) {
                var opt = Object.values(sel.tomselect.options).find(o => o.text === 'T-Shirt');
                if (opt) sel.tomselect.setValue(opt.value);
            }
        }");
        var catValue = await Page.EvaluateAsync<string>("() => document.getElementById('category-select')?.value || ''");
        Assert.That(catValue, Is.Not.Empty, "Category ID secilmeli");

        // Step 1 submit
        await Page.Locator("[data-wizard-step='1'] button[type='submit']").ClickAndWaitForHtmxAsync(Page, 10000);

        // Step 2: Ozellikler
        await Expect(Page.Locator("[data-wizard-step='2']")).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Zorunlu select alanlari doldur
        var requiredSelects = Page.Locator("[data-wizard-step='2'] select[required]");
        var selectCount = await requiredSelects.CountAsync();
        for (var i = 0; i < selectCount; i++)
        {
            var select = requiredSelects.Nth(i);
            var options = select.Locator("option:not([value=''])");
            var optionCount = await options.CountAsync();
            if (optionCount > 0)
            {
                var firstValue = await options.First.GetAttributeAsync("value");
                if (!string.IsNullOrEmpty(firstValue))
                    await select.SelectOptionAsync(firstValue);
            }
        }

        // Zorunlu text input doldur
        var requiredInputs = Page.Locator("[data-wizard-step='2'] input[type='text'][required]");
        var inputCount = await requiredInputs.CountAsync();
        for (var i = 0; i < inputCount; i++)
        {
            var input = requiredInputs.Nth(i);
            var currentValue = await input.InputValueAsync();
            if (string.IsNullOrWhiteSpace(currentValue))
                await input.FillAsync("E2E Test Deger");
        }

        // Step 2 submit
        await Page.Locator("[data-wizard-step='2'] button[type='submit']").ClickAndWaitForHtmxAsync(Page, 10000);

        // Step 3: Varyantlar
        await Expect(Page.Locator("[data-wizard-step='3']")).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Tom Select ile varyant degerleri sec (ilk 2 deger)
        await Page.EvaluateAsync(@"() => {
            var selects = document.querySelectorAll('.variant-tomselect');
            selects.forEach(function(sel) {
                if (!sel.tomselect) return;
                var ts = sel.tomselect;
                var opts = Object.values(ts.options);
                if (opts.length >= 2) {
                    ts.addItem(opts[0].value);
                    ts.addItem(opts[1].value);
                } else if (opts.length >= 1) {
                    ts.addItem(opts[0].value);
                }
            });
        }");

        // Tag input varsa deger ekle
        var tagInputs = Page.Locator(".variant-tag-input .tag-text-input");
        var tagCount = await tagInputs.CountAsync();
        for (var i = 0; i < tagCount; i++)
        {
            var tagInput = tagInputs.Nth(i);
            await tagInput.FillAsync("TestRenk1");
            await tagInput.PressAsync("Enter");
            await tagInput.FillAsync("TestRenk2");
            await tagInput.PressAsync("Enter");
        }

        // Varsayilan degerleri doldur
        await Page.FillAsync("input[name='DefaultValues.ListPrice']", "199.90");
        await Page.FillAsync("input[name='DefaultValues.SalePrice']", "149.90");
        await Page.FillAsync("input[name='DefaultValues.CostPrice']", "80");
        await Page.FillAsync("input[name='DefaultValues.DefaultStock']", "25");

        // Varyantlari olustur
        await Page.ClickAsync("#btn-generate-variants");
        await Page.Locator("#variant-table-container table").WaitForAsync(new() { Timeout = 10000 });

        // Depo stok gir (tum stok inputlarina 10)
        await Page.EvaluateAsync(@"() => {
            document.querySelectorAll('#variant-table-container input[name*=""BranchOfficeStocks""][name*="".Stock""]')
                .forEach(function(input) { input.value = '10'; });
        }");

        // Step 3 next
        var step3Next = Page.Locator("#btn-step3-next");
        await Expect(step3Next).ToBeEnabledAsync(new() { Timeout = 5000 });
        await step3Next.ClickAndWaitForHtmxAsync(Page, 10000);

        // Step 4: Gorseller (opsiyonel — direkt gecis)
        await Expect(Page.Locator("[data-wizard-step='4']")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Page.Locator("[data-wizard-step='4'] button[type='submit']").ClickAndWaitForHtmxAsync(Page, 10000);

        // Step 5: Onay
        await Expect(Page.Locator("[data-wizard-step='5']")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(Page.Locator(".datagrid")).ToBeVisibleAsync();

        // Devam: Yayinla
        await Page.Locator("[data-wizard-step='5'] button[type='submit']").ClickAndWaitForHtmxAsync(Page, 10000);

        // Step 6: Yayinla (atla)
        await Expect(Page.Locator("[data-wizard-step='6']")).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Atla, Kaydet (ilk submit butonu)
        var skipButton = Page.Locator("[data-wizard-step='6'] button[type='submit']").First;
        await skipButton.ClickAndWaitForHtmxAsync(Page, 15000);

        // Step 7: Basari
        await Expect(Page.Locator("[data-wizard-step='7']")).ToBeVisibleAsync(new() { Timeout = 15000 });
        await Expect(Page.GetByText("basariyla eklendi")).ToBeVisibleAsync();

        // Navigasyon butonlari gorunmeli
        await Expect(Page.GetByText("Senkronizasyona Git")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Urun Listesine Don")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Yeni Urun Ekle")).ToBeVisibleAsync();
    }

    [Test, Order(2)]
    public async Task Step1_BrandQuickAdd_WorksViaModal()
    {
        await Page.GotoAsync($"{BaseUrl}/products/add");
        await Expect(Page.Locator("[data-wizard-step='1']")).ToBeVisibleAsync();

        // + butonuna bas
        await Page.ClickAsync("#brand-add-btn");

        // Modal gorunmeli
        var modal = Page.Locator("#brand-add-modal");
        await Expect(modal).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Marka adini gir
        var brandInput = modal.Locator("#brand-add-input");
        await brandInput.FillAsync("E2ETestMarka" + DateTime.Now.Ticks);

        // Ekle butonuna bas
        await modal.Locator("#brand-add-confirm").ClickAsync();

        // Basari mesaji gorunmeli
        await Expect(modal.Locator("#brand-add-success")).ToBeVisibleAsync(new() { Timeout = 5000 });
    }

    [Test, Order(3)]
    public async Task Step1_TomSelect_SearchWorks()
    {
        await Page.GotoAsync($"{BaseUrl}/products/add");
        await Expect(Page.Locator("[data-wizard-step='1']")).ToBeVisibleAsync();

        // Tom Select'in yuklendigini dogrula
        var hasTomSelect = await Page.EvaluateAsync<bool>(@"() => {
            var sel = document.getElementById('brand-select');
            return sel && sel.tomselect !== undefined;
        }");
        Assert.That(hasTomSelect, Is.True, "Tom Select yuklu olmali");

        // Arama yap
        await Page.EvaluateAsync(@"() => {
            var sel = document.getElementById('brand-select');
            var ts = sel.tomselect;
            ts.open();
            ts.setTextboxValue('Ni');
            ts.refreshOptions();
        }");
        await Page.WaitForTimeoutAsync(300);

        // Filtrelenmis sonuc sayisini dogrula
        var visibleCount = await Page.EvaluateAsync<int>(@"() => {
            var sel = document.getElementById('brand-select');
            var ts = sel.tomselect;
            return ts.dropdown.querySelectorAll('.option:not(.ts-hidden-accessible)').length;
        }");
        Assert.That(visibleCount, Is.GreaterThan(0), "'Ni' aramasinda sonuc donmeli");
    }

    [Test, Order(4)]
    public async Task Step3_VariantRemoval_Works()
    {
        // Step 1 → Step 2 → Step 3'e hizli gecis
        await Page.GotoAsync($"{BaseUrl}/products/add");
        await Expect(Page.Locator("[data-wizard-step='1']")).ToBeVisibleAsync();

        await Page.FillAsync("input[name='Title']", "Variant Removal Test");
        await Page.EvaluateAsync(@"() => {
            var brand = document.getElementById('brand-select');
            if (brand?.tomselect) { var o = Object.values(brand.tomselect.options)[1]; if(o) brand.tomselect.setValue(o.value); }
            var cat = document.getElementById('category-select');
            if (cat?.tomselect) { var o = Object.values(cat.tomselect.options)[1]; if(o) cat.tomselect.setValue(o.value); }
        }");
        await Page.Locator("[data-wizard-step='1'] button[type='submit']").ClickAndWaitForHtmxAsync(Page, 10000);

        // Step 2 — zorunlu alanlari doldur ve devam
        await Expect(Page.Locator("[data-wizard-step='2']")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Page.EvaluateAsync(@"() => {
            document.querySelectorAll('[data-wizard-step=""2""] select[required]').forEach(function(sel) {
                if (sel.options.length > 1) { sel.selectedIndex = 1; sel.dispatchEvent(new Event('change')); }
            });
        }");
        await Page.Locator("[data-wizard-step='2'] button[type='submit']").ClickAndWaitForHtmxAsync(Page, 10000);

        // Step 3
        await Expect(Page.Locator("[data-wizard-step='3']")).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Varyant degerlerini sec + varsayilanlari doldur + olustur
        await Page.EvaluateAsync(@"() => {
            var selects = document.querySelectorAll('.variant-tomselect');
            selects.forEach(function(sel) {
                if (!sel.tomselect) return;
                var opts = Object.values(sel.tomselect.options);
                if (opts.length >= 2) { sel.tomselect.addItem(opts[0].value); sel.tomselect.addItem(opts[1].value); }
            });
            document.querySelector('[name=""DefaultValues.ListPrice""]').value = '100';
            document.querySelector('[name=""DefaultValues.SalePrice""]').value = '90';
            document.querySelector('[name=""DefaultValues.DefaultStock""]').value = '5';
        }");

        await Page.ClickAsync("#btn-generate-variants");
        await Page.Locator("#variant-table-container table").WaitForAsync(new() { Timeout = 10000 });

        // Varyant sayisini kontrol et
        var initialCount = await Page.Locator("#variant-table-container tbody tr").CountAsync();
        Assert.That(initialCount, Is.GreaterThan(1), "Birden fazla varyant olmali");

        // Ilk varyanti kaldir
        await Page.Locator("#variant-table-container .btn-remove-variant").First.ClickAsync();
        await Page.WaitForTimeoutAsync(300);

        var afterCount = await Page.Locator("#variant-table-container tbody tr").CountAsync();
        Assert.That(afterCount, Is.EqualTo(initialCount - 1), "Bir varyant kaldirilmali");
    }
}
