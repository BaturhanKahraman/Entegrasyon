using Entegrasyon.Business.Helpers;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Logs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Business.Concrete;

public sealed class CategoryAttributeValueManager : ICategoryAttributeValueManager
{
    // NormalizedName kolonu varchar(256). Normalize (upper) çok-baytlı karakterde büyüyebilir;
    // 256 altında headroom bırak ki DB hatası (500) yerine temiz validasyon mesajı dönsün.
    private const int MaxNameLength = 200;

    private readonly IDbContextFactory<IntegrationDbContext> _contextFactory;
    private readonly IApplicationLogManager _applicationLogManager;
    private readonly ILogger<CategoryAttributeValueManager> _logger;

    public CategoryAttributeValueManager(
        IDbContextFactory<IntegrationDbContext> contextFactory,
        IApplicationLogManager applicationLogManager,
        ILogger<CategoryAttributeValueManager> logger)
    {
        _contextFactory = contextFactory;
        _applicationLogManager = applicationLogManager;
        _logger = logger;
    }

    public async Task<IEnumerable<CategoryAttributeValue>> GetValuesByCategoryAttributeId(int id)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.CategoryAttributeValues.Where(x=>x.CategoryAttributeId== id).ToListAsync();
    }

    public async Task<IEnumerable<CategoryAttributeValue>> GetValuesByCategoryAttributeIds(IEnumerable<int> categoryAttributeIds)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        return await dbContext.CategoryAttributeValues
            .Where(cav=>categoryAttributeIds.Contains(cav.CategoryAttributeId))
            .ToListAsync();
    }

    public async Task<int> GetOrCreate(int categoryAttributeId, string rawName)
    {
        var normalized = AttributeValueNormalizer.Normalize(rawName);
        if (normalized.Length == 0)
            throw new ArgumentException("Değer boş olamaz.", nameof(rawName));
        if (rawName.Trim().Length > MaxNameLength)
            throw new ArgumentException($"Değer adı en fazla {MaxNameLength} karakter olabilir.", nameof(rawName));

        await using var dbContext = await _contextFactory.CreateDbContextAsync();

        var existing = await dbContext.CategoryAttributeValues
            .Where(v => v.CategoryAttributeId == categoryAttributeId && v.NormalizedName == normalized && !v.IsDeleted)
            .Select(v => (int?)v.Id)
            .FirstOrDefaultAsync();
        if (existing.HasValue)
            return existing.Value;

        var entity = new CategoryAttributeValue
        {
            CategoryAttributeId = categoryAttributeId,
            Name = rawName.Trim(),
            NormalizedName = normalized
        };
        dbContext.CategoryAttributeValues.Add(entity);
        try
        {
            await dbContext.SaveChangesAsync();
            return entity.Id;
        }
        catch (DbUpdateException)
        {
            // Race: another request inserted the same canonical value -> unique index violation. Return existing id.
            await using var retry = await _contextFactory.CreateDbContextAsync();
            return await retry.CategoryAttributeValues
                .Where(v => v.CategoryAttributeId == categoryAttributeId && v.NormalizedName == normalized && !v.IsDeleted)
                .Select(v => v.Id)
                .FirstAsync();
        }
    }

    public async Task<bool> UpdateName(int id, int categoryAttributeId, string newName)
    {
        var trimmed = newName?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            throw new ArgumentException("Değer adı boş olamaz.", nameof(newName));
        if (trimmed.Length > MaxNameLength)
            throw new ArgumentException($"Değer adı en fazla {MaxNameLength} karakter olabilir.", nameof(newName));

        var normalized = AttributeValueNormalizer.Normalize(trimmed);

        await _applicationLogManager.AddLog($"Özellik değeri güncelleniyor (Id: {id})", LogType.Category, LogAction.Update);
        _logger.LogInformation("Updating CategoryAttributeValue {ValueId} to name '{Name}'", id, trimmed);

        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        // IDOR koruması: değer hem id hem categoryAttributeId ile eşleşmeli.
        var entity = await dbContext.CategoryAttributeValues
            .AsTracking()
            .FirstOrDefaultAsync(v => v.Id == id && v.CategoryAttributeId == categoryAttributeId && !v.IsDeleted);

        if (entity is null)
        {
            _logger.LogWarning("CategoryAttributeValue {ValueId} (attr {AttrId}) not found for rename", id, categoryAttributeId);
            return false;
        }

        entity.Name = trimmed;
        entity.NormalizedName = normalized;
        await dbContext.SaveChangesAsync();

        await _applicationLogManager.AddLog($"Özellik değeri güncellendi: '{trimmed}' (Id: {id})", LogType.Category, LogAction.Update);
        _logger.LogInformation("CategoryAttributeValue {ValueId} renamed successfully", id);
        return true;
    }

    public async Task<bool> SoftDelete(int id, int categoryAttributeId)
    {
        await _applicationLogManager.AddLog($"Özellik değeri siliniyor (Id: {id})", LogType.Category, LogAction.Delete);
        _logger.LogInformation("Soft-deleting CategoryAttributeValue {ValueId}", id);

        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        // IDOR koruması: değer hem id hem categoryAttributeId ile eşleşmeli.
        var entity = await dbContext.CategoryAttributeValues
            .AsTracking()
            .FirstOrDefaultAsync(v => v.Id == id && v.CategoryAttributeId == categoryAttributeId && !v.IsDeleted);

        if (entity is null)
        {
            _logger.LogWarning("CategoryAttributeValue {ValueId} (attr {AttrId}) not found or already deleted", id, categoryAttributeId);
            return false;
        }

        entity.IsDeleted = true;
        entity.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        await _applicationLogManager.AddLog($"Özellik değeri silindi (Id: {id})", LogType.Category, LogAction.Delete);
        _logger.LogInformation("CategoryAttributeValue {ValueId} soft-deleted", id);
        return true;
    }
}
