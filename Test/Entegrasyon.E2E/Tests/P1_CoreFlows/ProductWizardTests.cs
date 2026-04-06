using System.Text.RegularExpressions;
using Entegrasyon.E2E.Infrastructure;

namespace Entegrasyon.E2E.Tests.P1_CoreFlows;

/// <summary>
/// Urun ekleme wizard'i (6 adimli) E2E testleri.
/// Akis: /products/add → Step 1 (temel bilgi) → Step 2 (ozellikler) → Step 3 (varyantlar)
///       → Step 4 (gorseller) → Step 5 (onay) → Step 6 (basari)
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
        var titleInput = Page.Locator("input[name='Title'], input#Title");
        await titleInput.FillAsync("E2E Test Urun " + DateTime.Now.Ticks);

        // Marka ara ve sec
        var brandInput = Page.Locator("#brand-input");
        await brandInput.FillAsync("Ni");
        await Page.WaitForTimeoutAsync(500); // debounce bekle
        var brandDropdown = Page.Locator("#brand-dropdown .dropdown-item");
        await brandDropdown.First.WaitForAsync(new() { Timeout = 5000 });
        await brandDropdown.First.ClickAsync();

        // Brand ID'nin dolup dolmadigini dogrula
        var brandId = Page.Locator("#brand-id");
        var brandIdValue = await brandId.InputValueAsync();
        Assert.That(string.IsNullOrEmpty(brandIdValue), Is.False, "Brand ID secilmeli");

        // Kategori ara ve sec
        var categoryInput = Page.Locator("#category-input");
        await categoryInput.FillAsync("T-Shirt");
        await Page.WaitForTimeoutAsync(500);
        var categoryDropdown = Page.Locator("#category-dropdown .dropdown-item");
        await categoryDropdown.First.WaitForAsync(new() { Timeout = 5000 });
        await categoryDropdown.First.ClickAsync();

        // Category ID'nin dolup dolmadigini dogrula
        var categoryId = Page.Locator("#category-id");
        var categoryIdValue = await categoryId.InputValueAsync();
        Assert.That(string.IsNullOrEmpty(categoryIdValue), Is.False, "Category ID secilmeli");

        // Step 1 submit
        var submitButton = Page.Locator("[data-wizard-step='1'] button[type='submit']");
        await submitButton.ClickAndWaitForHtmxAsync(Page, 10000);

        // Step 2: Ozellikler
        await Expect(Page.Locator("[data-wizard-step='2']")).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Zorunlu alanlari doldur (select ve input)
        var requiredSelects = Page.Locator("[data-wizard-step='2'] select[required]");
        var selectCount = await requiredSelects.CountAsync();
        for (var i = 0; i < selectCount; i++)
        {
            var select = requiredSelects.Nth(i);
            // Son option'i sec (genelde gecerli bir deger)
            var options = select.Locator("option:not([value=''])");
            var optionCount = await options.CountAsync();
            if (optionCount > 0)
            {
                var lastValue = await options.Last.GetAttributeAsync("value");
                if (!string.IsNullOrEmpty(lastValue))
                    await select.SelectOptionAsync(lastValue);
            }
        }

        var requiredInputs = Page.Locator("[data-wizard-step='2'] input[required]");
        var inputCount = await requiredInputs.CountAsync();
        for (var i = 0; i < inputCount; i++)
        {
            var input = requiredInputs.Nth(i);
            var currentValue = await input.InputValueAsync();
            if (string.IsNullOrWhiteSpace(currentValue))
                await input.FillAsync("E2E Test Deger");
        }

        // Step 2 submit
        var step2Submit = Page.Locator("[data-wizard-step='2'] button[type='submit']");
        await step2Submit.ClickAndWaitForHtmxAsync(Page, 10000);

        // Step 3: Varyantlar
        await Expect(Page.Locator("[data-wizard-step='3']")).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Default degerleri doldur
        var listPriceInput = Page.Locator("input[name='DefaultValues.ListPrice']");
        if (await listPriceInput.CountAsync() > 0)
            await listPriceInput.FillAsync("199.99");

        var salePriceInput = Page.Locator("input[name='DefaultValues.SalePrice']");
        if (await salePriceInput.CountAsync() > 0)
            await salePriceInput.FillAsync("149.99");

        var stockInput = Page.Locator("input[name='DefaultValues.Quantity']");
        if (await stockInput.CountAsync() > 0)
            await stockInput.FillAsync("10");

        // Varyant checkbox'larini sec (varsa)
        var checkboxes = Page.Locator(".attr-checkbox");
        var checkboxCount = await checkboxes.CountAsync();
        for (var i = 0; i < checkboxCount; i++)
        {
            var checkbox = checkboxes.Nth(i);
            if (!await checkbox.IsCheckedAsync())
                await checkbox.CheckAsync();
        }

        // Varyantlari olustur (JS fetch, HTMX degil)
        var generateButton = Page.Locator("#btn-generate");
        if (await generateButton.IsVisibleAsync() && await generateButton.IsEnabledAsync())
        {
            await generateButton.ClickAsync();
            // Variant tablosu olusana kadar bekle
            await Page.Locator("#variant-table-container table")
                .WaitForAsync(new() { Timeout = 10000 });
        }

        // Step 3 next
        var step3Next = Page.Locator("#btn-step3-next");
        await Expect(step3Next).ToBeEnabledAsync(new() { Timeout = 5000 });
        await step3Next.ClickAndWaitForHtmxAsync(Page, 10000);

        // Step 4: Gorseller (opsiyonel — direkt gecis)
        await Expect(Page.Locator("[data-wizard-step='4']")).ToBeVisibleAsync(new() { Timeout = 10000 });

        var step4Submit = Page.Locator("[data-wizard-step='4'] button[type='submit']");
        await step4Submit.ClickAndWaitForHtmxAsync(Page, 10000);

        // Step 5: Onay
        await Expect(Page.Locator("[data-wizard-step='5']")).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Datagrid ile ozet bilgileri gorunmeli
        await Expect(Page.Locator(".datagrid")).ToBeVisibleAsync();

        // Kaydet
        var saveButton = Page.Locator("button.btn-success");
        await saveButton.ClickAndWaitForHtmxAsync(Page, 15000);

        // Step 6: Basari
        await Expect(Page.Locator("[data-wizard-step='6']")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(Page.GetByText("basariyla eklendi")).ToBeVisibleAsync();
    }

    [Test, Order(2)]
    public async Task Step1_BrandNotFound_ShowsAddModal()
    {
        await Page.GotoAsync($"{BaseUrl}/products/add");
        await Expect(Page.Locator("[data-wizard-step='1']")).ToBeVisibleAsync();

        // Var olmayan bir marka adi gir
        var brandInput = Page.Locator("#brand-input");
        await brandInput.FillAsync("NonExistentBrand12345");
        await Page.WaitForTimeoutAsync(500); // debounce bekle

        // Baska bir alana tiklayarak blur tetikle
        var titleInput = Page.Locator("input[name='Title'], input#Title");
        await titleInput.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Marka ekleme modali gorunmeli
        var modal = Page.Locator("#brand-add-modal");
        await Expect(modal).ToBeVisibleAsync(new() { Timeout = 5000 });

        // Modal icinde "bulunamadi" mesaji olmali
        await Expect(modal.GetByText(new Regex("markasi bulunamadi", RegexOptions.IgnoreCase)))
            .ToBeVisibleAsync();
    }

    [Test, Order(3)]
    public async Task Step1_SearchBrand_ShowsResults()
    {
        await Page.GotoAsync($"{BaseUrl}/products/add");
        await Expect(Page.Locator("[data-wizard-step='1']")).ToBeVisibleAsync();

        // "Ni" ile arama yap
        var brandInput = Page.Locator("#brand-input");
        await brandInput.FillAsync("Ni");
        await Page.WaitForTimeoutAsync(500); // debounce bekle

        // Dropdown sonuclari gorunmeli
        var dropdownItems = Page.Locator("#brand-dropdown .dropdown-item");
        await dropdownItems.First.WaitForAsync(new() { Timeout = 5000 });

        var count = await dropdownItems.CountAsync();
        Assert.That(count, Is.GreaterThan(0), "'Ni' aramasinda sonuc donmeli");

        // Nike iceren sonuc olmali
        var nikeItem = dropdownItems.Filter(new() { HasText = "Nike" });
        Assert.That(await nikeItem.CountAsync(), Is.GreaterThan(0),
            "Sonuclarda 'Nike' olmali");
    }
}
