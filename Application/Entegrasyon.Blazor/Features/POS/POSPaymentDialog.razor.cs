using System.Globalization;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.POS;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.POS;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.POS;

public partial class POSPaymentDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private IPOSSessionManager POSSessionManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    [Parameter] public decimal GrandTotal { get; set; }
    [Parameter] public List<POSPage.CartItem> CartItems { get; set; } = [];
    [Parameter] public POSSession ActiveSession { get; set; } = null!;
    [Parameter] public Guid CurrentUserId { get; set; }
    [Parameter] public int BranchOfficeId { get; set; }

    private static readonly CultureInfo _trCulture = new("tr-TR");

    private PaymentMethod _selectedPaymentMethod = PaymentMethod.Cash;
    private decimal _cashReceived;
    private string? _cardAuthCode;
    private decimal _cashPortion;
    private bool _processing;

    private bool CanConfirm => _selectedPaymentMethod switch
    {
        PaymentMethod.Cash => _cashReceived >= GrandTotal,
        PaymentMethod.Mixed => _cashPortion >= 0 && _cashPortion <= GrandTotal,
        _ => true
    };

    protected override void OnParametersSet()
    {
        _cashReceived = GrandTotal;
        _cashPortion = 0;
    }

    private void Cancel() => MudDialog.Cancel();

    private async Task Confirm()
    {
        _processing = true;
        try
        {
            var saleItems = CartItems.Select(item => new SaleItemDto(
                item.ProductVariantId,
                (double)item.VatRate,
                item.DiscountPercent,
                item.UnitPrice,
                item.Quantity,
                string.Empty
            ));

            var makeSaleDto = new MakeSaleDto(
                CurrentUserId,
                1, // Default walk-in customer
                0, // No general discount
                BranchOfficeId,
                saleItems
            );

            var cashReceived = _selectedPaymentMethod switch
            {
                PaymentMethod.Cash => _cashReceived,
                PaymentMethod.Mixed => _cashPortion,
                _ => 0m
            };

            var transactionDto = new POSTransactionDto(
                ActiveSession.Id,
                makeSaleDto,
                _selectedPaymentMethod,
                cashReceived,
                _cardAuthCode
            );

            var result = await POSSessionManager.RecordTransactionAsync(transactionDto);
            if (result.Success)
            {
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                Snackbar.Add(result.Message ?? "Odeme islemi basarisiz.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            _processing = false;
        }
    }
}
