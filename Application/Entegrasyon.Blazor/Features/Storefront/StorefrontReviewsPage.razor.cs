using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Storefront;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Storefront;

public partial class StorefrontReviewsPage
{
    [Inject] private IStorefrontReviewManager ReviewManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    private List<StorefrontReview> _reviews = [];
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadReviewsAsync();
    }

    private async Task LoadReviewsAsync()
    {
        _loading = true;
        var result = await ReviewManager.GetAllReviewsAsync(1);
        if (result.Success)
            _reviews = result.Data;
        _loading = false;
    }

    private async Task ApproveAsync(StorefrontReview review)
    {
        var result = await ReviewManager.ApproveReviewAsync(review.Id);
        Snackbar.Add(result.Message ?? (result.Success ? "Onaylandi." : "Hata."),
            result.Success ? Severity.Success : Severity.Error);
        await LoadReviewsAsync();
    }

    private async Task RejectAsync(StorefrontReview review)
    {
        var confirm = await DialogService.ShowMessageBox(
            "Yorum Reddet",
            "Bu yorumu reddetmek istediginizden emin misiniz?",
            yesText: "Reddet",
            cancelText: "Iptal");

        if (confirm == true)
        {
            var result = await ReviewManager.RejectReviewAsync(review.Id);
            Snackbar.Add(result.Message ?? (result.Success ? "Reddedildi." : "Hata."),
                result.Success ? Severity.Success : Severity.Error);
            await LoadReviewsAsync();
        }
    }

    private async Task OpenReplyDialog(StorefrontReview review)
    {
        var parameters = new DialogParameters
        {
            { "ContentText", "Yanit yazin:" },
            { "ButtonText", "Gonder" },
            { "Color", Color.Primary }
        };

        var replyText = review.ReplyText ?? "";
        var result = await DialogService.ShowMessageBox(
            "Yoruma Yanit",
            new MarkupString($"<p>Yorum: {review.Comment}</p>"),
            yesText: "Kaydet",
            cancelText: "Iptal");

        if (result == true)
        {
            var reply = await ReviewManager.ReplyToReviewAsync(review.Id, "Tesekkurler, degerlendirmeniz icin.");
            Snackbar.Add(reply.Message ?? "Yanit kaydedildi.", Severity.Success);
            await LoadReviewsAsync();
        }
    }
}
