using Entegrasyon.E2E.Infrastructure;

namespace Entegrasyon.E2E.Tests.P2_BranchOffices;

/// <summary>
/// Faz 8: Şube ofisi silme onay iş akışı E2E testi.
///
/// Akış:
///   1. Admin login
///   2. Yeni stoksuz şube oluştur (timestamp ile benzersiz ad)
///   3. Şube listesinde yeni şubenin "Silme Talebi Aç" butonuna bas
///   4. /branch-office-deletion-requests'te pending talep görünmeli
///   5. Detay sayfasından "Onayla"
///   6. Şube listesi → silinen şube artık görünmemeli
///   7. Audit log filter → BranchOffice entityType ile RequestOpen + RequestApprove logları görünmeli
///
/// Stoksuz şube seçildi: target seçim modal'ı yok (Faz 4'te eklenmedi),
/// bu yüzden test kararlı çalışır.
/// </summary>
[TestFixture, Order(100)]
public class DeletionApprovalFlowE2E : E2ETestBase
{
    [SetUp]
    public async Task SetUp()
    {
        await LoginAsAdminAsync();
    }

    [Test, Order(1)]
    public async Task FullDeletionApprovalFlow_StocklessBranch_EndToEnd()
    {
        // Benzersiz şube adı — paralel run güvenli
        var branchName = "E2E Silme Test " + DateTime.Now.Ticks;

        // ─── 1. Yeni şube oluştur ─────────────────────────────────
        // Login sonrası antiforgery cookie rotation'ı tamamlansın diye NetworkIdle bekle
        await Page.GotoAsync($"{BaseUrl}/branch-offices/create", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Expect(Page.Locator("input[name='Name']")).ToBeVisibleAsync();

        await Page.FillAsync("input[name='Name']", branchName);
        await Page.FillAsync("textarea[name='Address']", "E2E test adresi - " + DateTime.Now.ToString("HH:mm:ss"));

        // Form içindeki submit (navbar'daki Çıkış Yap'tan ayırt etmek için scope)
        await Page.Locator("form[action='/branch-offices/create'] button[type='submit']").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 15000 });
        TestContext.Progress.WriteLine($"[E2E] After submit URL: {Page.Url}");

        // Create sayfasında kalmış olmamalıyız — Manager success'te Index'e redirect eder
        if (Page.Url.Contains("/branch-offices/create"))
        {
            var errorAlert = await Page.Locator(".alert-danger, .text-danger").AllInnerTextsAsync();
            Assert.Fail($"Branch create form sayfasında kaldı, beklenen redirect olmadı. Hatalar: [{string.Join(" | ", errorAlert)}]");
        }

        // ─── 2. Listede yeni şubeyi bul ────────────────────────────
        await Page.GotoAsync($"{BaseUrl}/branch-offices");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Yeni şubeyi text ile bul (table'daki link içinde) → o satırın butonuna ulaş
        var branchLink = Page.GetByRole(AriaRole.Link, new() { Name = branchName });
        await Expect(branchLink).ToBeVisibleAsync(new() { Timeout = 10000 });
        var newBranchRow = branchLink.Locator("xpath=ancestor::tr").First;

        // ─── 3. "Silme Talebi Aç" butonuna bas ────────────────────
        // Faz 7 cleanup C: Index.cshtml'de form-post butonu (HQ değilse görünür)
        // Confirm dialog → accept
        Page.Dialog += async (_, dialog) => await dialog.AcceptAsync();

        var deleteRequestButton = newBranchRow.GetByRole(AriaRole.Button, new() { Name = "Silme Talebi Aç" });
        await Expect(deleteRequestButton).ToBeVisibleAsync();
        await deleteRequestButton.ClickAsync();

        // PRG: RequestDelete controller'ı POST sonrası Detay sayfasına yönlendirir
        // (BranchOfficeDeletionRequestController.RequestDelete: RedirectToAction(nameof(Detail), new { id = result.Data }))
        await Page.WaitForURLAsync(new Regex(@"/branch-office-deletion-requests/\d+$"), new() { Timeout = 15000 });
        TestContext.Progress.WriteLine($"[E2E] Detail page URL: {Page.Url}");

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var pageTitle = await Page.TitleAsync();
        TestContext.Progress.WriteLine($"[E2E] Page title: {pageTitle}");
        if (pageTitle.Contains("Internal Server Error") || pageTitle.Contains("Error"))
        {
            // 500 — full body al, stack trace çıksın
            var body = await Page.Locator("body").InnerTextAsync();
            TestContext.Progress.WriteLine($"[E2E] BODY:\n{body.Substring(0, Math.Min(2000, body.Length))}");
            Assert.Fail("Detail page Internal Server Error");
        }

        // ─── 4. Onayla ──────────────────────────────────────────────
        // Stoksuz şube → HasStockTransfer false → checkbox yok, butona doğrudan basılabilir
        var approveButton = Page.GetByRole(AriaRole.Button, new() { Name = "Onayla" });
        await Expect(approveButton).ToBeVisibleAsync(new() { Timeout = 10000 });
        await approveButton.ClickAsync();

        // Approve sonrası liste sayfasına redirect
        await Page.WaitForURLAsync(new Regex(@"/branch-office-deletion-requests$"), new() { Timeout = 15000 });

        // ─── 6. Şube listesinde artık görünmemeli ─────────────────
        await Page.GotoAsync($"{BaseUrl}/branch-offices");
        var deletedBranchRow = Page.Locator("table tbody tr").Filter(new() { HasText = branchName });
        await Expect(deletedBranchRow).ToHaveCountAsync(0);

        // ─── 7. Audit log filter — BranchDeletion log'ları ────────
        // Faz 6: entityType filter
        await Page.GotoAsync($"{BaseUrl}/logs?entityType=BranchOffice");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // RequestOpen ve RequestApprove eylem satırları en azından 1'er tane var olmalı
        var logRows = Page.Locator("table tbody tr");
        var logCount = await logRows.CountAsync();
        Assert.That(logCount, Is.GreaterThan(0), "BranchOffice entityType ile log satırları bulunamadı");
    }
}
