using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Storefront;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Storefront;

public partial class StorefrontPaymentPage
{
    [Inject] private IDbContextFactory<IntegrationDbContext> ContextFactory { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private StorefrontPaymentConfig _config = new() { TenantId = 1 };
    private bool _loading = true;
    private bool _saving;
    private bool _testing;

    protected override async Task OnInitializedAsync()
    {
        await using var dbContext = await ContextFactory.CreateDbContextAsync();
        var existing = await dbContext.StorefrontPaymentConfigs
            .FirstOrDefaultAsync(c => c.TenantId == 1);

        if (existing is not null)
            _config = existing;

        _loading = false;
    }

    private async Task SaveAsync()
    {
        _saving = true;

        try
        {
            await using var dbContext = await ContextFactory.CreateDbContextAsync();

            if (_config.Id == 0)
            {
                _config.TenantId = 1;
                dbContext.StorefrontPaymentConfigs.Add(_config);
            }
            else
            {
                dbContext.StorefrontPaymentConfigs.Update(_config);
            }

            await dbContext.SaveChangesAsync();
            Snackbar.Add("Odeme ayarlari kaydedildi.", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Hata: {ex.Message}", Severity.Error);
        }

        _saving = false;
    }

    private async Task TestConnectionAsync()
    {
        _testing = true;

        try
        {
            if (string.IsNullOrWhiteSpace(_config.ApiKey) || string.IsNullOrWhiteSpace(_config.SecretKey))
            {
                Snackbar.Add("API Key ve Secret Key girilmeli.", Severity.Warning);
                return;
            }

            var baseUrl = _config.BaseUrl
                ?? (_config.IsLive ? "https://api.iyzipay.com" : "https://sandbox-api.iyzipay.com");

            var options = new Iyzipay.Options
            {
                ApiKey = _config.ApiKey,
                SecretKey = _config.SecretKey,
                BaseUrl = baseUrl
            };

            // Use InstallmentInfo as a lightweight connectivity check (ApiTest not available in SDK 2.x)
            var request = new Iyzipay.Request.RetrieveInstallmentInfoRequest
            {
                Locale = Iyzipay.Model.Locale.TR.ToString(),
                ConversationId = Guid.NewGuid().ToString(),
                BinNumber = "454360", // Visa test BIN
                Price = "100.00"
            };

            var result = await Iyzipay.Model.InstallmentInfo.Retrieve(request, options);

            if (result.Status == "success")
                Snackbar.Add("iyzico baglantisi basarili!", Severity.Success);
            else
                Snackbar.Add($"iyzico hatasi: {result.ErrorMessage}", Severity.Error);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Baglanti hatasi: {ex.Message}", Severity.Error);
        }
        finally
        {
            _testing = false;
        }
    }
}
