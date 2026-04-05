namespace Entegrasyon.E2E.PageObjects;

/// <summary>
/// /auth/login sayfasının Page Object Model'i.
/// MVC form: label "Kullanıcı Adı", "Şifre"; buton: "Giriş Yap".
/// Hata: .alert-danger; başarı: PRG redirect → /.
/// </summary>
public class LoginPage(IPage page, string baseUrl)
{
    public ILocator UsernameField => page.GetByLabel("Kullanıcı Adı");
    public ILocator PasswordField => page.GetByLabel("Şifre");
    public ILocator LoginButton => page.GetByRole(AriaRole.Button, new() { Name = "Giriş Yap" });
    public ILocator ErrorAlert => page.Locator(".alert-danger");
    public ILocator PageTitle => page.GetByText("Entegrasyon");

    public async Task NavigateAsync()
    {
        await page.GotoAsync($"{baseUrl}/auth/login");
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await LoginButton.WaitForAsync(new() { Timeout = 15000 });
    }

    public async Task LoginAsync(string username, string password)
    {
        await UsernameField.FillAsync(username);
        await PasswordField.FillAsync(password);
        await LoginButton.ClickAsync();
    }

    public async Task LoginAndWaitForDashboardAsync(string username, string password)
    {
        await LoginAsync(username, password);
        await page.WaitForURLAsync($"{baseUrl}/", new() { Timeout = 30000 });
    }
}
