using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Invoicing;
using Entegrasyon.Entity.Invoicing;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Invoicing;

public partial class InvoiceDetailDialog
{
    [Inject] private IEInvoiceManager EInvoiceManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter] public Guid InvoiceId { get; set; }

    private EInvoiceDetailDto? _detail;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadDetail();
    }

    private async Task LoadDetail()
    {
        _loading = true;
        var result = await EInvoiceManager.GetInvoiceDetail(InvoiceId);
        if (result.Success)
            _detail = result.Data;
        _loading = false;
    }

    private async Task SendToGib()
    {
        var result = await EInvoiceManager.SendToGib(InvoiceId);
        if (result.Success)
        {
            Snackbar.Add("Fatura GIB'e gonderildi.", Severity.Success);
            await LoadDetail();
        }
        else
        {
            Snackbar.Add(result.Message ?? "Gonderim basarisiz.", Severity.Error);
        }
    }

    private async Task CancelInvoice()
    {
        var result = await EInvoiceManager.CancelInvoice(InvoiceId);
        if (result.Success)
        {
            Snackbar.Add("Fatura iptal edildi.", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        else
        {
            Snackbar.Add(result.Message ?? "Iptal basarisiz.", Severity.Error);
        }
    }

    private void Close() => MudDialog.Cancel();

    private static Color GetStatusColor(EInvoiceStatus status) => status switch
    {
        EInvoiceStatus.Draft => Color.Default,
        EInvoiceStatus.Sent => Color.Info,
        EInvoiceStatus.Accepted => Color.Success,
        EInvoiceStatus.Rejected => Color.Error,
        EInvoiceStatus.Cancelled => Color.Warning,
        _ => Color.Default
    };

    private static string GetStatusText(EInvoiceStatus status) => status switch
    {
        EInvoiceStatus.Draft => "Taslak",
        EInvoiceStatus.Sent => "Gonderildi",
        EInvoiceStatus.Accepted => "Kabul Edildi",
        EInvoiceStatus.Rejected => "Reddedildi",
        EInvoiceStatus.Cancelled => "Iptal",
        _ => "Bilinmiyor"
    };
}
