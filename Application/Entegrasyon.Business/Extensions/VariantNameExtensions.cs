using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Business.Extensions;

public static class VariantNameExtensions
{
    /// <summary>
    /// Variant display name'ini resolve eder. Oncelik sirasi: storedName > compute from attributes > productTitle.
    /// Asla null donmez.
    /// </summary>
    public static string ResolveDisplayName(
        string? storedName,
        IEnumerable<VariantAttributeLite>? attributes,
        string productTitle)
    {
        if (!string.IsNullOrWhiteSpace(storedName)) return storedName.Trim();

        if (attributes is not null)
        {
            var values = attributes
                .Where(a => a.IsVarianter || a.IsSlicer)
                .OrderByDescending(a => a.IsVarianter)
                .ThenBy(a => a.Order)
                .Select(a => a.Value ?? a.CustomValue ?? string.Empty)
                .Where(v => !string.IsNullOrWhiteSpace(v));

            var computed = string.Join(" ", values).Trim();
            if (!string.IsNullOrWhiteSpace(computed)) return computed;
        }

        return productTitle;
    }
}
