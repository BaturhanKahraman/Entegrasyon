using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Shipping;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Shipping;

public partial class AddTrackingDialog : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Inject]
    private IShipmentTrackingManager ShipmentTrackingManager { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    private MudForm _form = null!;
    private int _selectedCargoCompanyId;
    private string _trackingNumber = string.Empty;
    private string? _errorMessage;
    private bool _isSubmitting;

    private async Task OnSubmit()
    {
        await _form.Validate();
        if (!_form.IsValid) return;

        _isSubmitting = true;
        _errorMessage = null;

        try
        {
            var dto = new TrackShipmentDto(_trackingNumber.Trim(), _selectedCargoCompanyId);
            var result = await ShipmentTrackingManager.TrackShipmentAsync(dto);

            if (result.Success)
            {
                Snackbar.Add("Kargo takibi basariyla eklendi", Severity.Success);
                MudDialog.Close(DialogResult.Ok(result.Data));
            }
            else
            {
                _errorMessage = result.Message;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Beklenmeyen hata: {ex.Message}";
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
