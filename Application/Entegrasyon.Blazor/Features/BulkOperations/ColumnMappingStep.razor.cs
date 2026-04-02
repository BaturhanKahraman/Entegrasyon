using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.BulkOperations;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.BulkOperations;

public partial class ColumnMappingStep
{
    [Inject] private IImportColumnMappingService MappingService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    [Parameter] public List<string> ExcelHeaders { get; set; } = [];
    [Parameter] public List<List<string>> PreviewRows { get; set; } = [];
    [Parameter] public BulkOperationType ImportType { get; set; }
    [Parameter] public EventCallback<Dictionary<string, string>> OnMappingConfirmed { get; set; }

    private List<ImportSystemField> _systemFields = [];
    private Dictionary<string, string?> _mappings = new();
    private List<ImportColumnProfile> _profiles = [];
    private int? _selectedProfileId;
    private bool _saveAsProfile;
    private string _profileName = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        _systemFields = MappingService.GetSystemFields(ImportType);
        _mappings = MappingService.SuggestMappings(ExcelHeaders, ImportType);
        _profiles = await MappingService.GetProfilesAsync(ImportType);
    }

    private Task LoadProfile(int? profileId)
    {
        if (profileId is null) return Task.CompletedTask;
        var profile = _profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile is null) return Task.CompletedTask;

        var mappings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(profile.MappingsJson);
        if (mappings is null) return Task.CompletedTask;

        foreach (var (key, value) in mappings)
        {
            if (_mappings.ContainsKey(key) && ExcelHeaders.Contains(value))
                _mappings[key] = value;
        }

        Snackbar.Add($"'{profile.Name}' profili yüklendi.", Severity.Success);
        return Task.CompletedTask;
    }

    private async Task DeleteProfile()
    {
        if (_selectedProfileId is null) return;
        var result = await MappingService.DeleteProfileAsync(_selectedProfileId.Value);
        if (result.Success)
        {
            _profiles = await MappingService.GetProfilesAsync(ImportType);
            _selectedProfileId = null;
            Snackbar.Add("Profil silindi.", Severity.Success);
        }
    }

    private async Task Confirm()
    {
        // Validate required fields
        var missingRequired = _systemFields
            .Where(f => f.IsRequired && string.IsNullOrWhiteSpace(_mappings.GetValueOrDefault(f.Key)))
            .Select(f => f.DisplayName)
            .ToList();

        if (missingRequired.Count > 0)
        {
            Snackbar.Add($"Zorunlu alanlar eşleştirilmeli: {string.Join(", ", missingRequired)}", Severity.Warning);
            return;
        }

        // Save profile if requested
        if (_saveAsProfile && !string.IsNullOrWhiteSpace(_profileName))
        {
            var confirmed = _mappings
                .Where(m => m.Value is not null)
                .ToDictionary(m => m.Key, m => m.Value!);
            await MappingService.SaveProfileAsync(_profileName, ImportType, confirmed);
        }

        var finalMapping = _mappings
            .Where(m => m.Value is not null)
            .ToDictionary(m => m.Key, m => m.Value!);

        await OnMappingConfirmed.InvokeAsync(finalMapping);
    }

    private string? GetPreviewValue(string? excelColumn)
    {
        if (excelColumn is null || PreviewRows.Count == 0) return null;
        var colIdx = ExcelHeaders.IndexOf(excelColumn);
        if (colIdx < 0 || colIdx >= PreviewRows[0].Count) return null;
        return PreviewRows[0][colIdx];
    }
}
