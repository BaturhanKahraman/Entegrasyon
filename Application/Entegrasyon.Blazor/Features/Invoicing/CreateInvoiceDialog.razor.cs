using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Invoicing;
using Entegrasyon.Entity.Invoicing;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Invoicing;

public partial class CreateInvoiceDialog
{
    [Inject] private IEInvoiceManager EInvoiceManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    private MudForm _form = null!;

    private EInvoiceType _invoiceType = EInvoiceType.EFatura;
    private IntegratorProvider _integratorProvider = IntegratorProvider.Custom;
    private string _customerTaxId = string.Empty;
    private string _customerTitle = string.Empty;
    private DateTime? _issueDate = DateTime.Today;

    private readonly List<InvoiceLineModel> _lines = [new()];

    private void AddLine() => _lines.Add(new InvoiceLineModel());

    private void RemoveLine(int index)
    {
        if (_lines.Count > 1)
            _lines.RemoveAt(index);
    }

    private async Task Submit()
    {
        var dto = new CreateEInvoiceDto
        {
            InvoiceType = _invoiceType,
            IntegratorProvider = _integratorProvider,
            CustomerTaxId = _customerTaxId,
            CustomerTitle = _customerTitle,
            IssueDate = _issueDate.HasValue
                ? new DateTimeOffset(_issueDate.Value, TimeSpan.Zero)
                : DateTimeOffset.UtcNow,
            Lines = _lines.Select(l => new CreateEInvoiceLineDto(
                l.ProductName, l.Quantity, l.UnitPrice, l.TaxRate)).ToList()
        };

        try
        {
            var result = await EInvoiceManager.CreateInvoice(dto);
            if (result.Success)
            {
                MudDialog.Close(DialogResult.Ok(result.Data));
            }
            else
            {
                Snackbar.Add(result.Message ?? "Fatura olusturulamadi.", Severity.Error);
            }
        }
        catch (FluentValidation.ValidationException ex)
        {
            foreach (var error in ex.Errors)
                Snackbar.Add(error.ErrorMessage, Severity.Warning);
        }
    }

    private void Cancel() => MudDialog.Cancel();

    private sealed class InvoiceLineModel
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public int TaxRate { get; set; } = 20;
    }
}
