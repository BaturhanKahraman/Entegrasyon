using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Matches;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.AttributeSync;

public partial class ValueMatchSection
{
    [Parameter] public int ApplicationAttributeId { get; set; }
    [Parameter] public List<AttributeValueInfo> AppValues { get; set; } = [];
    [Parameter] public List<MarketplaceAttributeValueDto> MarketplaceValues { get; set; } = [];
    [Parameter] public Dictionary<int, CategoryAttributeValueMarketPlaceMatch> ExistingValueMatches { get; set; } = new();
    [Parameter] public int MarketPlaceId { get; set; }
    [Parameter] public EventCallback OnValueMatchChanged { get; set; }

    [Inject] private IAttributeMatchManager AttributeMatchManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private Dictionary<int, int?> _pendingSelections = new();
    private bool _isProcessing;

    private int MatchedCount => AppValues.Count(v => ExistingValueMatches.ContainsKey(v.ValueId));

    private void SetPendingSelection(int valueId, int? mpValueId)
    {
        _pendingSelections[valueId] = mpValueId;
    }

    private IEnumerable<MarketplaceAttributeValueDto> GetAvailableMarketplaceValues(int appValueId)
    {
        // Exclude marketplace values already used by other app values
        var usedMpValueIds = ExistingValueMatches
            .Where(kvp => kvp.Key != appValueId)
            .Select(kvp => kvp.Value.MarketPlaceCategoryAttributeValueId)
            .ToHashSet();

        return MarketplaceValues.Where(v => !usedMpValueIds.Contains(v.Id));
    }

    private async Task SaveValueMatch(int appValueId)
    {
        if (!_pendingSelections.TryGetValue(appValueId, out var mpValueId) || mpValueId is null)
            return;

        _isProcessing = true;
        var result = await AttributeMatchManager.SaveValueMatchAsync(appValueId, MarketPlaceId, mpValueId.Value);
        _isProcessing = false;

        if (result.Success)
        {
            _pendingSelections.Remove(appValueId);
            Snackbar.Add("Değer eşleştirmesi kaydedildi.", Severity.Success);
            await OnValueMatchChanged.InvokeAsync();
        }
        else
        {
            Snackbar.Add($"Hata: {result.Message}", Severity.Error);
        }
    }

    private async Task RemoveValueMatch(int appValueId)
    {
        _isProcessing = true;
        var result = await AttributeMatchManager.RemoveValueMatchAsync(appValueId, MarketPlaceId);
        _isProcessing = false;

        if (result.Success)
        {
            Snackbar.Add("Değer eşleştirmesi kaldırıldı.", Severity.Info);
            await OnValueMatchChanged.InvokeAsync();
        }
        else
        {
            Snackbar.Add($"Hata: {result.Message}", Severity.Error);
        }
    }

    private async Task AutoMatchAll()
    {
        _isProcessing = true;

        var unmatchedValues = AppValues.Where(v => !ExistingValueMatches.ContainsKey(v.ValueId)).ToList();
        var savedCount = 0;

        foreach (var appVal in unmatchedValues)
        {
            // Try exact name match first
            var mpVal = MarketplaceValues.FirstOrDefault(mv =>
                string.Equals(mv.Name, appVal.ValueName, StringComparison.OrdinalIgnoreCase));

            if (mpVal is not null)
            {
                // Check not already used
                var isUsed = ExistingValueMatches.Values.Any(m => m.MarketPlaceCategoryAttributeValueId == mpVal.Id);
                if (!isUsed)
                {
                    var result = await AttributeMatchManager.SaveValueMatchAsync(appVal.ValueId, MarketPlaceId, mpVal.Id);
                    if (result.Success)
                        savedCount++;
                }
            }
        }

        _isProcessing = false;

        if (savedCount > 0)
        {
            Snackbar.Add($"{savedCount} değer otomatik eşleştirildi.", Severity.Success);
            await OnValueMatchChanged.InvokeAsync();
        }
        else
        {
            Snackbar.Add("Otomatik eşleştirilecek değer bulunamadı.", Severity.Info);
        }
    }
}
