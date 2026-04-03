using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Label;
using Entegrasyon.Entity.Labels;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

public class LabelTemplateManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IFluentValidator validator,
    IApplicationLogManager logManager,
    ILogger<LabelTemplateManager> logger) : ILabelTemplateService
{
    public async Task<IDataResult<List<LabelTemplateDto>>> GetAllAsync()
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var templates = await dbContext.LabelTemplates
            .Where(t => !t.IsDeleted)
            .OrderByDescending(t => t.IsDefault)
            .ThenBy(t => t.Name)
            .ToListAsync();

        var dtos = templates.Select(MapToDto).ToList();
        return new SuccessDataResult<List<LabelTemplateDto>>(dtos);
    }

    public async Task<IDataResult<LabelTemplateDto>> GetByIdAsync(Guid id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var template = await dbContext.LabelTemplates
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        if (template is null)
            return new ErrorDataResult<LabelTemplateDto>(null!, "Şablon bulunamadı");

        return new SuccessDataResult<LabelTemplateDto>(MapToDto(template));
    }

    public async Task<IDataResult<LabelTemplateDto>> GetDefaultAsync(LabelType type)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var template = await dbContext.LabelTemplates
            .FirstOrDefaultAsync(t => t.Type == type && t.IsDefault && !t.IsDeleted);

        if (template is null)
            return new ErrorDataResult<LabelTemplateDto>(null!, "Varsayılan şablon bulunamadı");

        return new SuccessDataResult<LabelTemplateDto>(MapToDto(template));
    }

    public async Task<IDataResult<LabelTemplateDto>> SaveAsync(SaveLabelTemplateDto dto)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        // 1. Validation
        var validationResult = await validator.Validate(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return new ErrorDataResult<LabelTemplateDto>(null!, errors);
        }

        // 2. Business Rules — aynı isimde şablon var mı
        var duplicateExists = await dbContext.LabelTemplates
            .AnyAsync(t => t.Name == dto.Name && t.Id != dto.Id && !t.IsDeleted);

        if (duplicateExists)
            return new ErrorDataResult<LabelTemplateDto>(null!, $"'{dto.Name}' adında bir şablon zaten mevcut");

        // 3. Execution
        var layoutJson = JsonSerializer.Serialize(dto.Elements);

        LabelTemplate template;

        if (dto.Id.HasValue && dto.Id.Value != Guid.Empty)
        {
            // Update
            template = (await dbContext.LabelTemplates
                .FirstOrDefaultAsync(t => t.Id == dto.Id.Value && !t.IsDeleted))!;

            if (template is null)
                return new ErrorDataResult<LabelTemplateDto>(null!, "Güncellenecek şablon bulunamadı");

            dbContext.Entry(template).State = EntityState.Modified;
            template.Name = dto.Name;
            template.Type = dto.Type;
            template.WidthMm = dto.WidthMm;
            template.HeightMm = dto.HeightMm;
            template.Dpi = dto.Dpi;
            template.LayoutJson = layoutJson;
            template.IsDefault = dto.IsDefault;
        }
        else
        {
            // Insert
            template = new LabelTemplate
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Type = dto.Type,
                WidthMm = dto.WidthMm,
                HeightMm = dto.HeightMm,
                Dpi = dto.Dpi,
                LayoutJson = layoutJson,
                IsDefault = dto.IsDefault
            };
            await dbContext.LabelTemplates.AddAsync(template);
        }

        // IsDefault ise aynı tipteki diğerlerini kapat
        if (template.IsDefault)
            await ClearDefaultForType(dbContext, template.Type, template.Id);

        await dbContext.SaveChangesAsync();

        await logManager.AddLog($"Etiket şablonu kaydedildi: {template.Name}",
            LogType.Product, LogAction.Add, dto);
        logger.LogInformation("Etiket şablonu kaydedildi: {TemplateId} - {Name}", template.Id, template.Name);

        return new SuccessDataResult<LabelTemplateDto>(MapToDto(template), "Şablon kaydedildi");
    }

    public async Task<IResult> DeleteAsync(Guid id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var template = await dbContext.LabelTemplates
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        if (template is null)
            return new ErrorResult("Şablon bulunamadı");

        dbContext.Entry(template).State = EntityState.Modified;
        template.IsDeleted = true;
        template.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        await logManager.AddLog($"Etiket şablonu silindi: {template.Name}",
            LogType.Product, LogAction.Delete);
        logger.LogInformation("Etiket şablonu silindi: {TemplateId}", id);

        return new SuccessResult("Şablon silindi");
    }

    public async Task<IResult> SetAsDefaultAsync(Guid id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var template = await dbContext.LabelTemplates
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        if (template is null)
            return new ErrorResult("Şablon bulunamadı");

        await ClearDefaultForType(dbContext, template.Type, template.Id);

        dbContext.Entry(template).State = EntityState.Modified;
        template.IsDefault = true;
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Varsayılan şablon değiştirildi: {TemplateId} - {Name}", id, template.Name);
        return new SuccessResult($"'{template.Name}' varsayılan şablon olarak ayarlandı");
    }

    private async Task ClearDefaultForType(IntegrationDbContext dbContext, LabelType type, Guid excludeId)
    {
        var defaults = await dbContext.LabelTemplates
            .Where(t => t.Type == type && t.IsDefault && t.Id != excludeId && !t.IsDeleted)
            .ToListAsync();

        foreach (var t in defaults)
        {
            dbContext.Entry(t).State = EntityState.Modified;
            t.IsDefault = false;
        }
    }

    private static LabelTemplateDto MapToDto(LabelTemplate template)
    {
        var elements = JsonSerializer.Deserialize<List<LabelElement>>(template.LayoutJson) ?? [];
        return new LabelTemplateDto(
            template.Id, template.Name, template.Type,
            template.WidthMm, template.HeightMm, template.Dpi,
            elements, template.IsDefault);
    }
}
