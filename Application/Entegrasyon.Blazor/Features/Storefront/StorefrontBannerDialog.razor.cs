using Entegrasyon.Entity.Storefront;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Storefront;

public partial class StorefrontBannerDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter] public StorefrontBanner Banner { get; set; } = new();
    [Parameter] public bool IsEdit { get; set; }

    private MudForm _form = null!;
    private bool _isValid;
    private DateTime? _startDate;
    private DateTime? _endDate;

    protected override void OnParametersSet()
    {
        _startDate = Banner.StartDate?.LocalDateTime;
        _endDate = Banner.EndDate?.LocalDateTime;
    }

    private void Cancel() => MudDialog.Cancel();

    private void Submit()
    {
        _form.Validate();
        if (!_isValid) return;

        Banner.StartDate = _startDate.HasValue
            ? new DateTimeOffset(_startDate.Value, TimeSpan.Zero)
            : null;
        Banner.EndDate = _endDate.HasValue
            ? new DateTimeOffset(_endDate.Value, TimeSpan.Zero)
            : null;

        MudDialog.Close(DialogResult.Ok(Banner));
    }
}
