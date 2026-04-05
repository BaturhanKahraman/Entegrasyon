namespace Entegrasyon.E2E.Infrastructure;

/// <summary>
/// MVC cookie auth login flow helper.
/// </summary>
public static class AuthHelper
{
    public static async Task LoginAsync(IPage page, string baseUrl, string username, string password)
    {
        await page.GotoAsync($"{baseUrl}/auth/login");
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await page.GetByLabel("Kullanıcı Adı").FillAsync(username);
        await page.GetByLabel("Şifre").FillAsync(password);

        await page.GetByRole(AriaRole.Button, new() { Name = "Giriş Yap" }).ClickAsync();

        // PRG: form POST → redirect to dashboard
        await page.WaitForURLAsync($"{baseUrl}/", new() { Timeout = 30000 });
    }
}
