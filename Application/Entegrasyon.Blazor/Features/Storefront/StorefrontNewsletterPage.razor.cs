using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Storefront;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.Storefront;

public partial class StorefrontNewsletterPage
{
    [Inject] private IStorefrontNewsletterManager NewsletterManager { get; set; } = null!;

    private List<StorefrontNewsletter> _subscribers = [];
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        _loading = true;
        var result = await NewsletterManager.GetSubscribersAsync(1);
        if (result.Success)
            _subscribers = result.Data;
        _loading = false;
    }
}
