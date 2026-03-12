using Entegrasyon.Business.Abstract;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using AppCategoryAttribute = Entegrasyon.Entity.Categories.CategoryAttribute;

namespace Entegrasyon.Blazor.Features.MarketplaceSync;

public partial class AttributeSync
{
    [Inject] private ICategoryAttributeManager AttributeManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private List<AttributeSyncRow> _attributes = [];
    private bool _isLoading = true;
    private bool _showUnmappedOnly;
    private int _matchedCount;

    private IEnumerable<AttributeSyncRow> FilteredRows =>
        _showUnmappedOnly ? _attributes.Where(a => !a.HasMatch) : _attributes;

    protected override async Task OnInitializedAsync()
    {
        await LoadAttributes();
    }

    private async Task LoadAttributes()
    {
        try
        {
            _isLoading = true;

            var attrsResult = await AttributeManager.GetCategoryAttributes();
            if (!attrsResult.Success || attrsResult.Data is null)
            {
                Snackbar.Add("Özellikler yüklenemedi.", Severity.Error);
                return;
            }

            var matchDict = await AttributeManager.GetAttributeMarketPlaceMatchesAsync();

            _attributes = attrsResult.Data.Select(attr =>
            {
                matchDict.TryGetValue(attr.Id, out var match);
                return new AttributeSyncRow
                {
                    Id = attr.Id,
                    Humanized = attr.CategoryAttributeHumanized,
                    Key = attr.CategoryAttributeKey,
                    ValueCount = attr.CategoryAttributeValues?.Count ?? 0,
                    HasMatch = match is not null,
                    MarketplaceName = match?.MarketplaceName,
                    MarketplaceAttributeId = match?.MarketplaceAttributeId
                };
            })
            .OrderBy(a => a.Humanized)
            .ToList();

            _matchedCount = _attributes.Count(a => a.HasMatch);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Özellikler yüklenirken hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    public class AttributeSyncRow
    {
        public int Id { get; set; }
        public string Humanized { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public int ValueCount { get; set; }
        public bool HasMatch { get; set; }
        public string? MarketplaceName { get; set; }
        public int? MarketplaceAttributeId { get; set; }
    }
}
