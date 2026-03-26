using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Storefront;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Storefront;

public partial class StorefrontEmailCampaignsPage
{
    [Inject] private IStorefrontCampaignManager CampaignManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private List<StorefrontEmailCampaign> _campaigns = [];
    private bool _loading = true;
    private bool _saving;

    // Create / Edit dialog
    private bool _dialogVisible;
    private StorefrontEmailCampaign? _editingCampaign;
    private string _dialogSubject = "";
    private string _dialogHtml = "";
    private CampaignTarget _dialogTarget = CampaignTarget.AllSubscribers;

    // Schedule dialog
    private bool _scheduleDialogVisible;
    private int _scheduleCampaignId;
    private DateTime? _scheduleDate;
    private TimeSpan? _scheduleTime;

    protected override async Task OnInitializedAsync()
    {
        await LoadCampaignsAsync();
    }

    private async Task LoadCampaignsAsync()
    {
        _loading = true;
        var result = await CampaignManager.GetCampaignsAsync(1);
        if (result.Success)
            _campaigns = result.Data;
        _loading = false;
    }

    private void OpenCreateDialog()
    {
        _editingCampaign = null;
        _dialogSubject = "";
        _dialogHtml = "";
        _dialogTarget = CampaignTarget.AllSubscribers;
        _dialogVisible = true;
    }

    private void OpenEditDialog(StorefrontEmailCampaign campaign)
    {
        _editingCampaign = campaign;
        _dialogSubject = campaign.Subject;
        _dialogHtml = campaign.HtmlContent;
        _dialogTarget = campaign.Target;
        _dialogVisible = true;
    }

    private void CloseDialog() => _dialogVisible = false;

    private async Task SaveDialogAsync()
    {
        _saving = true;

        if (_editingCampaign is not null)
        {
            var result = await CampaignManager.UpdateCampaignAsync(
                _editingCampaign.Id, _dialogSubject, _dialogHtml, _dialogTarget);
            Snackbar.Add(result.Message ?? (result.Success ? "Guncellendi" : "Hata"), result.Success ? Severity.Success : Severity.Error);
        }
        else
        {
            var result = await CampaignManager.CreateCampaignAsync(
                1, _dialogSubject, _dialogHtml, _dialogTarget);
            Snackbar.Add(result.Message ?? (result.Success ? "Olusturuldu" : "Hata"), result.Success ? Severity.Success : Severity.Error);
        }

        _saving = false;
        _dialogVisible = false;
        await LoadCampaignsAsync();
    }

    private void OpenScheduleDialog(StorefrontEmailCampaign campaign)
    {
        _scheduleCampaignId = campaign.Id;
        _scheduleDate = DateTime.Now.AddDays(1);
        _scheduleTime = new TimeSpan(10, 0, 0);
        _scheduleDialogVisible = true;
    }

    private async Task ScheduleAsync()
    {
        if (_scheduleDate is null || _scheduleTime is null)
        {
            Snackbar.Add("Tarih ve saat secimi zorunludur.", Severity.Warning);
            return;
        }

        _saving = true;
        var dateTime = _scheduleDate.Value.Date + _scheduleTime.Value;
        var scheduleAt = new DateTimeOffset(dateTime, TimeSpan.FromHours(3)); // TR timezone offset

        var result = await CampaignManager.ScheduleCampaignAsync(_scheduleCampaignId, scheduleAt);
        Snackbar.Add(result.Message ?? (result.Success ? "Zamanlandi" : "Hata"), result.Success ? Severity.Success : Severity.Error);

        _saving = false;
        _scheduleDialogVisible = false;
        await LoadCampaignsAsync();
    }

    private async Task CancelCampaignAsync(int id)
    {
        var result = await CampaignManager.CancelCampaignAsync(id);
        Snackbar.Add(result.Message ?? (result.Success ? "Iptal edildi" : "Hata"), result.Success ? Severity.Success : Severity.Error);
        await LoadCampaignsAsync();
    }

    private async Task DeleteCampaignAsync(int id)
    {
        var result = await CampaignManager.DeleteCampaignAsync(id);
        Snackbar.Add(result.Message ?? (result.Success ? "Silindi" : "Hata"), result.Success ? Severity.Success : Severity.Error);
        await LoadCampaignsAsync();
    }

    private static string GetStatusLabel(CampaignStatus status) => status switch
    {
        CampaignStatus.Draft => "Taslak",
        CampaignStatus.Scheduled => "Zamanlanmis",
        CampaignStatus.Sending => "Gonderiliyor",
        CampaignStatus.Sent => "Gonderildi",
        CampaignStatus.Cancelled => "Iptal",
        _ => status.ToString()
    };

    private static Color GetStatusColor(CampaignStatus status) => status switch
    {
        CampaignStatus.Draft => Color.Default,
        CampaignStatus.Scheduled => Color.Info,
        CampaignStatus.Sending => Color.Warning,
        CampaignStatus.Sent => Color.Success,
        CampaignStatus.Cancelled => Color.Error,
        _ => Color.Default
    };

    private static string GetTargetLabel(CampaignTarget target) => target switch
    {
        CampaignTarget.AllSubscribers => "Tum Aboneler",
        CampaignTarget.AllCustomers => "Tum Musteriler",
        CampaignTarget.ActiveCustomers => "Aktif Musteriler",
        _ => target.ToString()
    };
}
