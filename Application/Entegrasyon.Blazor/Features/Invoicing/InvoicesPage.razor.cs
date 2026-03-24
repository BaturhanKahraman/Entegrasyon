using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Invoicing;
using Entegrasyon.Entity.Invoicing;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Invoicing;

public partial class InvoicesPage
{
    [Inject] private IEInvoiceManager EInvoiceManager { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private List<EInvoiceListDto> _invoices = [];
    private bool _loading = true;
    private int _currentPage = 1;
    private int _totalPages = 1;
    private const int PageSize = 20;

    private EInvoiceStatus? _statusFilter;
    private EInvoiceType? _typeFilter;
    private string? _searchTerm;

    protected override async Task OnInitializedAsync()
    {
        await LoadInvoices();
    }

    private async Task LoadInvoices()
    {
        _loading = true;
        var filter = new EInvoiceFilterDto
        {
            PageIndex = _currentPage - 1,
            PageSize = PageSize,
            Status = _statusFilter,
            InvoiceType = _typeFilter,
            SearchTerm = _searchTerm
        };

        var result = await EInvoiceManager.GetInvoices(filter);
        if (result.Success)
        {
            _invoices = result.Data.Items.ToList();
            _totalPages = (int)Math.Ceiling((double)result.Data.TotalItemCount / PageSize);
            if (_totalPages == 0) _totalPages = 1;
        }
        _loading = false;
    }

    private async Task OnStatusFilterChanged(EInvoiceStatus? value)
    {
        _statusFilter = value;
        _currentPage = 1;
        await LoadInvoices();
    }

    private async Task OnTypeFilterChanged(EInvoiceType? value)
    {
        _typeFilter = value;
        _currentPage = 1;
        await LoadInvoices();
    }

    private async Task OnSearchChanged(string value)
    {
        _searchTerm = value;
        _currentPage = 1;
        await LoadInvoices();
    }

    private async Task OnPageChanged(int page)
    {
        _currentPage = page;
        await LoadInvoices();
    }

    private async Task OpenCreateDialog()
    {
        var dialog = await DialogService.ShowAsync<CreateInvoiceDialog>("Yeni Fatura",
            new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true });
        var dialogResult = await dialog.Result;
        if (dialogResult is { Canceled: false })
        {
            await LoadInvoices();
            Snackbar.Add("Fatura basariyla olusturuldu.", Severity.Success);
        }
    }

    private async Task OnRowClick(DataGridRowClickEventArgs<EInvoiceListDto> args)
    {
        var parameters = new DialogParameters<InvoiceDetailDialog>
        {
            { x => x.InvoiceId, args.Item.Id }
        };
        var dialog = await DialogService.ShowAsync<InvoiceDetailDialog>("Fatura Detayi", parameters,
            new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true });
        var dialogResult = await dialog.Result;
        if (dialogResult is { Canceled: false })
        {
            await LoadInvoices();
        }
    }

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
