namespace Entegrasyon.E2E.PageObjects;

/// <summary>
/// /auth/login sayfasının Page Object Model'i.
/// MudTextField label'ları: "Kullanıcı Adı", "Şifre"; buton: "Giriş Yap".
/// </summary>
public class LoginPage(IPage page, string baseUrl)
{
    public ILocator UsernameField => page.GetByLabel("Kullanıcı Adı");
    public ILocator PasswordField => page.GetByLabel("Şifre");
    public ILocator LoginButton => page.GetByRole(AriaRole.Button, new() { Name = "Giriş Yap" });
    public ILocator LoadingText => page.GetByText("Giriş Yapılıyor...");
    public ILocator Snackbar => page.Locator(".mud-snackbar");
    public ILocator PageTitle => page.GetByText("Entegrasyon");

    public async Task NavigateAsync()
    {
        await page.GotoAsync($"{baseUrl}/auth/login");
        await PageTitle.WaitForAsync(new() { Timeout = 15000 });
    }

    public async Task LoginAsync(string username, string password)
    {
        await UsernameField.FillAsync(username);
        await PasswordField.FillAsync(password);

        // Butonun validation sonrası aktif olmasını bekle
        await Assertions.Expect(LoginButton).ToBeEnabledAsync(new() { Timeout = 5000 });
        await LoginButton.ClickAsync();
    }

    public async Task LoginAndWaitForDashboardAsync(string username, string password)
    {
        await LoginAsync(username, password);
        await page.WaitForURLAsync($"{baseUrl}/", new() { Timeout = 30000 });
    }
}
