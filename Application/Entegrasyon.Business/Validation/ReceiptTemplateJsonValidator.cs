using System.Text.Json;
using Entegrasyon.Entity.Dtos.Receipts;

namespace Entegrasyon.Business.Validation;

public readonly record struct ValidationOutcome(bool IsValid, string? Error);

public static class ReceiptTemplateJsonValidator
{
    public static ValidationOutcome ValidateThermal(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return new(false, "Thermal JSON root must be an array.");

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var err = ValidateBlock(element);
                if (err is not null) return new(false, err);
            }
            return new(true, null);
        }
        catch (JsonException ex) { return new(false, $"Invalid JSON: {ex.Message}"); }
    }

    public static ValidationOutcome ValidateA4(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return new(false, "A4 JSON root must be an object.");

            foreach (var property in doc.RootElement.EnumerateObject())
            {
                if (property.Name is not ("hl" or "hr" or "body" or "fl" or "fr"))
                    return new(false, $"Unknown A4 zone '{property.Name}'.");
                if (property.Value.ValueKind != JsonValueKind.Array)
                    return new(false, $"A4 zone '{property.Name}' must be an array.");

                foreach (var element in property.Value.EnumerateArray())
                {
                    var err = ValidateBlock(element);
                    if (err is not null) return new(false, err);
                }
            }
            return new(true, null);
        }
        catch (JsonException ex) { return new(false, $"Invalid JSON: {ex.Message}"); }
    }

    private static string? ValidateBlock(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) return "Block must be an object.";
        if (!element.TryGetProperty("type", out var typeEl) || typeEl.ValueKind != JsonValueKind.String)
            return "Block missing 'type'.";
        var type = typeEl.GetString()!;
        if (!ReceiptBlockTypes.All.Contains(type)) return $"Unknown block type '{type}'.";
        return null;
    }
}
