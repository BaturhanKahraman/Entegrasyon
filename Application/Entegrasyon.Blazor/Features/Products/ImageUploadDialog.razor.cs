using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Products;

public partial class ImageUploadDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public List<VariantInfo> Variants { get; set; } = [];
    [Parameter] public Dictionary<int, List<ImageItem>> ExistingImages { get; set; } = new();

    // All images in the gallery (pool)
    private readonly List<ImageItem> _allImages = [];

    // variantIdx → set of imageIds assigned to that variant
    private readonly Dictionary<int, HashSet<Guid>> _assignments = new();

    // variantIdx → coverId (the primary/cover image for that variant)
    private readonly Dictionary<int, Guid?> _covers = new();

    protected override void OnInitialized()
    {
        // Seed from existing images
        if (ExistingImages is { Count: > 0 })
        {
            foreach (var (variantIdx, items) in ExistingImages)
            {
                foreach (var item in items)
                {
                    // Avoid duplicate images in pool (same Id)
                    if (_allImages.All(i => i.Id != item.Id))
                        _allImages.Add(item);

                    // Mark assignment
                    if (!_assignments.ContainsKey(variantIdx))
                        _assignments[variantIdx] = [];
                    _assignments[variantIdx].Add(item.Id);

                    // Mark cover
                    if (item.IsPrimary)
                        _covers[variantIdx] = item.Id;
                }
            }
        }
    }

    private async Task OnFilesUploaded(IReadOnlyList<IBrowserFile> files)
    {
        foreach (var file in files)
        {
            if (file.Size > 5_242_880) continue; // skip files > 5MB

            using var ms = new MemoryStream();
            await file.OpenReadStream(maxAllowedSize: 5_242_880).CopyToAsync(ms);
            var bytes = ms.ToArray();
            var previewUrl = $"data:{file.ContentType};base64,{Convert.ToBase64String(bytes)}";

            var imageItem = new ImageItem
            {
                Id = Guid.NewGuid(),
                FileData = bytes,
                FileName = file.Name,
                PreviewUrl = previewUrl
            };
            _allImages.Add(imageItem);

            // Auto-assign to all variants if there's only one variant
            if (Variants is { Count: 1 })
            {
                var vIdx = Variants[0].Index;
                if (!_assignments.ContainsKey(vIdx))
                    _assignments[vIdx] = [];
                _assignments[vIdx].Add(imageItem.Id);

                // First image becomes cover
                if (!_covers.ContainsKey(vIdx) || _covers[vIdx] is null)
                    _covers[vIdx] = imageItem.Id;
            }
        }

        StateHasChanged();
    }

    private void RemoveImage(ImageItem img)
    {
        _allImages.Remove(img);

        // Remove from all assignments
        foreach (var (_, set) in _assignments)
            set.Remove(img.Id);

        // Clear cover references
        foreach (var key in _covers.Keys.ToList())
        {
            if (_covers[key] == img.Id)
                _covers[key] = null;
        }
    }

    private bool IsAssigned(int variantIdx, Guid imageId)
        => _assignments.TryGetValue(variantIdx, out var set) && set.Contains(imageId);

    private bool IsCover(int variantIdx, Guid imageId)
        => _covers.TryGetValue(variantIdx, out var coverId) && coverId == imageId;

    private void ToggleAssignment(int variantIdx, Guid imageId, bool assigned)
    {
        if (!_assignments.ContainsKey(variantIdx))
            _assignments[variantIdx] = [];

        if (assigned)
        {
            _assignments[variantIdx].Add(imageId);

            // If no cover yet, set this as cover
            if (!_covers.ContainsKey(variantIdx) || _covers[variantIdx] is null)
                _covers[variantIdx] = imageId;
        }
        else
        {
            _assignments[variantIdx].Remove(imageId);

            // If removed image was cover, clear it or pick another
            if (_covers.TryGetValue(variantIdx, out var coverId) && coverId == imageId)
            {
                _covers[variantIdx] = _assignments[variantIdx].Count > 0
                    ? _assignments[variantIdx].First()
                    : null;
            }
        }
    }

    private void SetCover(int variantIdx, Guid imageId)
    {
        _covers[variantIdx] = imageId;
    }

    private void Save()
    {
        var result = new Dictionary<int, List<ImageItem>>();

        foreach (var variant in Variants)
        {
            var vIdx = variant.Index;
            if (!_assignments.TryGetValue(vIdx, out var assignedIds) || assignedIds.Count == 0)
                continue;

            var coverId = _covers.GetValueOrDefault(vIdx);
            var items = assignedIds
                .Select(id => _allImages.FirstOrDefault(i => i.Id == id))
                .Where(i => i is not null)
                .Select(i => new ImageItem
                {
                    Id = i!.Id,
                    FileData = i.FileData,
                    FileName = i.FileName,
                    PreviewUrl = i.PreviewUrl,
                    IsPrimary = i.Id == coverId
                })
                .ToList();

            if (items.Count > 0)
                result[vIdx] = items;
        }

        MudDialog.Close(DialogResult.Ok(result));
    }

    private void Cancel() => MudDialog.Cancel();

    // ── Inner classes ──

    public class VariantInfo
    {
        public int Index { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public class ImageItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public byte[] FileData { get; set; } = [];
        public string FileName { get; set; } = string.Empty;
        public string PreviewUrl { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
    }
}
