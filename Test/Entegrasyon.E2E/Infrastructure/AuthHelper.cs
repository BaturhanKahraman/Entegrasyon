namespace Entegrasyon.E2E.Infrastructure;

/// <summary>
/// Blazor Server login flow helper.
/// ProtectedLocalStorage şifreli session kullandığı için gerçek login flow zorunlu.
/// </summary>
public static class AuthHelper
{
    public static async Task LoginAsync(IPage page, string baseUrl, string username, string password)
    {
        await page.GotoAsync($"{baseUrl}/auth/login");

        // Blazor SignalR bağlantısının kurulmasını bekle
        await page.WaitForSelectorAsync("text=Giriş Yap", new() { Timeout = 15000 });

        // Immediate="true" sayesinde FillAsync oninput event'ini tetikler
        // ve MudForm validation anında güncellenir
        await page.GetByLabel("Kullanıcı Adı").FillAsync(username);
        await page.GetByLabel("Şifre").FillAsync(password);

        // Butonun aktif olmasını bekle (validation geçmeli)
        var loginButton = page.GetByRole(AriaRole.Button, new() { Name = "Giriş Yap" });
        await Expect(loginButton).ToBeEnabledAsync(new() { Timeout = 5000 });
        await loginButton.ClickAsync();

        // Dashboard'a yönlendirilmeyi bekle
        await page.WaitForURLAsync($"{baseUrl}/", new() { Timeout = 30000 });
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
