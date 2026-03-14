using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Label;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Printing;

public partial class PrintButton
{
    [Parameter] public Guid VariantId { get; set; }
    [Parameter] public string Label { get; set; } = "Barkod Yazdır";
    [Parameter] public Variant Variant { get; set; } = Variant.Text;
    [Parameter] public Color Color { get; set; } = Color.Default;
    [Parameter] public Size Size { get; set; } = Size.Medium;

    [Inject] private ILabelService LabelService { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private bool _processing;

    private async Task HandlePrint()
    {
        if (VariantId == Guid.Empty) return;

        _processing = true;
        try
        {
            var result = await LabelService.GenerateProductLabel(VariantId);
            if (!result.Success || result.Data is null)
            {
                Snackbar.Add(result.Message ?? "Etiket üretilemedi", Severity.Error);
                return;
            }

            var parameters = new DialogParameters<PrintDialog>
            {
                { x => x.PrintJob, result.Data }
            };

            await DialogService.ShowAsync<PrintDialog>("Barkod Yazdır", parameters,
                new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true });
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
