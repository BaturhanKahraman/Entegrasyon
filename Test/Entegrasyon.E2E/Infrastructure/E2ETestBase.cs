using Microsoft.Extensions.Configuration;

namespace Entegrasyon.E2E.Infrastructure;

/// <summary>
/// Tüm E2E testlerinin base class'ı.
/// Playwright NUnit PageTest'ten türer — her test için otomatik browser context yönetimi sağlar.
/// </summary>
public class E2ETestBase : PageTest
{
    protected string BaseUrl { get; private set; } = null!;

    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.e2e.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    [OneTimeSetUp]
    public void BaseOneTimeSetUp()
    {
        BaseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL")
                  ?? Configuration["E2E:BaseUrl"]
                  ?? "http://localhost:5100";
    }

    [SetUp]
    public async Task BaseSetUp()
    {
        await Context.Tracing.StartAsync(new()
        {
            Screenshots = true,
            Snapshots = true,
            Sources = false
        });
    }

    [TearDown]
    public async Task BaseTearDown()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.ResultState.Failure.Status)
        {
            var artifactsDir = Path.Combine(
                TestContext.CurrentContext.WorkDirectory, "playwright-artifacts");
            Directory.CreateDirectory(artifactsDir);

            var testName = TestContext.CurrentContext.Test.Name;
            var timestamp = DateTime.Now.ToString("HHmmss");

            var screenshotPath = Path.Combine(artifactsDir, $"{testName}_{timestamp}.png");
            await Page.ScreenshotAsync(new() { Path = screenshotPath, FullPage = true });
            TestContext.AddTestAttachment(screenshotPath, "Screenshot on failure");

            var tracePath = Path.Combine(artifactsDir, $"{testName}_{timestamp}.zip");
            await Context.Tracing.StopAsync(new() { Path = tracePath });
            TestContext.AddTestAttachment(tracePath, "Playwright trace");
        }
        else
        {
            await Context.Tracing.StopAsync();
        }
    }

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            Locale = "tr-TR",
            TimezoneId = "Europe/Istanbul",
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors = true
        };
    }

    /// <summary>
    /// Login olmuş bir sayfa döner. Çoğu test bunun üzerinden başlar.
    /// </summary>
    protected async Task LoginAsAdminAsync()
    {
        await AuthHelper.LoginAsync(Page, BaseUrl, TestData.TestUsers.AdminUsername, TestData.TestUsers.AdminPassword);
    }
}
