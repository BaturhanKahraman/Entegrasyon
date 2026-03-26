using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Storefront;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Storefront;

public partial class StorefrontLegalPage
{
    [Inject] private IStorefrontSettingsManager SettingsManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private List<LegalTab> _legalTabs = [];
    private bool _loading = true;
    private bool _saving;

    protected override async Task OnInitializedAsync()
    {
        var result = await SettingsManager.GetByTenantIdAsync(1);
        var settings = result.Success ? result.Data : new StorefrontSettings { TenantId = 1 };

        _legalTabs =
        [
            new("Hakkimizda", "AboutHtml", settings.AboutHtml ?? string.Empty),
            new("Iade Politikasi", "ReturnPolicyHtml", settings.ReturnPolicyHtml ?? string.Empty),
            new("Gizlilik Politikasi", "PrivacyPolicyHtml", settings.PrivacyPolicyHtml ?? string.Empty),
            new("Kullanim Kosullari", "TermsHtml", settings.TermsHtml ?? string.Empty),
            new("KVKK", "KvkkHtml", settings.KvkkHtml ?? string.Empty),
            new("Cerez Politikasi", "CookiePolicyHtml", settings.CookiePolicyHtml ?? string.Empty),
            new("Mesafeli Satis Sozlesmesi", "DistanceSalesContractHtml", settings.DistanceSalesContractHtml ?? string.Empty),
            new("On Bilgilendirme Formu", "PreInfoFormHtml", settings.PreInfoFormHtml ?? string.Empty),
            new("Teslimat Kosullari", "DeliveryTermsHtml", settings.DeliveryTermsHtml ?? string.Empty),
        ];

        _loading = false;
    }

    private async Task SaveLegalTextAsync(LegalTab tab)
    {
        _saving = true;

        var result = await SettingsManager.UpdateLegalTextAsync(1, tab.FieldName, tab.Content);
        if (result.Success)
            Snackbar.Add($"{tab.DisplayName} kaydedildi.", Severity.Success);
        else
            Snackbar.Add(result.Message ?? "Hata olustu.", Severity.Error);

        _saving = false;
    }

    public class LegalTab(string displayName, string fieldName, string content)
    {
        public string DisplayName { get; } = displayName;
        public string FieldName { get; } = fieldName;
        public string Content { get; set; } = content;
    }
}
