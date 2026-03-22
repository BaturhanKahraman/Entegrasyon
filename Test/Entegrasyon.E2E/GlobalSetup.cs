using Entegrasyon.E2E.TestData;

namespace Entegrasyon.E2E;

/// <summary>
/// Tüm E2E testlerinden ÖNCE bir kez çalışır.
/// DB'ye test verilerini seed eder.
/// NUnit [SetUpFixture] assembly seviyesinde çalışır.
/// </summary>
[SetUpFixture]
public class GlobalSetup
{
    [OneTimeSetUp]
    public async Task RunBeforeAllTests()
    {
        try
        {
            await TestDataSeeder.SeedAsync();
            TestContext.Progress.WriteLine("E2E test verileri başarıyla seed edildi.");
        }
        catch (Exception ex)
        {
            TestContext.Progress.WriteLine($"UYARI: Test verisi seed başarısız — {ex.Message}");
            TestContext.Progress.WriteLine("Testler seed verisi olmadan devam edecek.");
        }
    }
}
