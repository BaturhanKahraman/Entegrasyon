using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Entegrasyon.MVC.Infrastructure.ModelBinding;

/// <summary>
/// decimal / decimal? alanları için hem InvariantCulture (nokta-ondalık "199.90") hem de
/// Türkçe (virgül-ondalık "199,90", binlik "1.234,56") biçimini tolere eden model binder.
///
/// Neden (T111, defense-in-depth): para inputları normalde site.js ile submit öncesi invariant
/// "1234.56"ya normalize edilir. Ama JS kapalıysa / HTMX kısmi post / API çağrısı ham Türkçe
/// değer gönderebilir; default invariant binder "199,90"yı yanlış parse edip ×100 veya hata
/// üretir. Bu binder kaynağa bakmadan değeri güvenli normalize edip parse eder.
///
/// Tek-nokta ("199.90", "1.234") durumu KASITLI olarak invariant ondalık kabul edilir — default
/// binder davranışıyla aynı (binlik mi ondalık mı ayrımı belirsiz; JS o yolu zaten halleder).
/// </summary>
public sealed class FlexibleDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var modelName = bindingContext.ModelName;
        var valueResult = bindingContext.ValueProvider.GetValue(modelName);
        if (valueResult == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(modelName, valueResult);

        var raw = valueResult.FirstValue;
        var underlyingType = Nullable.GetUnderlyingType(bindingContext.ModelType) ?? bindingContext.ModelType;

        if (string.IsNullOrWhiteSpace(raw))
        {
            // Boş + nullable → null; boş + non-nullable → değer yok (required validation'a bırak)
            if (underlyingType != bindingContext.ModelType)
                bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        if (TryParseFlexible(raw, out var value))
        {
            bindingContext.Result = ModelBindingResult.Success(value);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(
                modelName,
                bindingContext.ModelMetadata.ModelBindingMessageProvider
                    .AttemptedValueIsInvalidAccessor(raw, bindingContext.ModelMetadata.GetDisplayName()));
        }

        return Task.CompletedTask;
    }

    /// <summary>"199,90", "1.234,56", "199.90", "1234" → 199.90 / 1234.56 / 199.90 / 1234.</summary>
    public static bool TryParseFlexible(string raw, out decimal value)
    {
        var s = raw.Trim();
        var hasDot = s.Contains('.');
        var hasComma = s.Contains(',');

        if (hasDot && hasComma)
        {
            // Son ayraç ondalıktır: "1.234,56" → virgül ondalık; "1,234.56" → nokta ondalık.
            if (s.LastIndexOf(',') > s.LastIndexOf('.'))
                s = s.Replace(".", "").Replace(',', '.'); // Türkçe/AB: nokta=binlik, virgül=ondalık
            else
                s = s.Replace(",", "");                    // US: virgül=binlik, nokta=ondalık
        }
        else if (hasComma)
        {
            // Yalnız virgül → ondalık virgül ("199,90" → "199.90")
            s = s.Replace(',', '.');
        }
        // Yalnız nokta / ayraçsız → invariant ondalık (default davranış)

        return decimal.TryParse(
            s,
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out value);
    }
}

/// <summary>decimal ve decimal? için <see cref="FlexibleDecimalModelBinder"/> sağlar.</summary>
public sealed class FlexibleDecimalModelBinderProvider : IModelBinderProvider
{
    private static readonly FlexibleDecimalModelBinder Binder = new();

    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var type = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;
        return type == typeof(decimal) ? Binder : null;
    }
}
