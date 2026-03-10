using System.Collections.Frozen;

namespace Entegrasyon.Entity;

public static class OperatorTypeStorage
{
    public static readonly Dictionary<FilterOperator, Type[]> OperatorTypes = new Dictionary<FilterOperator, Type[]>
    {
        { FilterOperator.Equals, new[] { typeof(string), typeof(int), typeof(DateTime) /* diğer türler */ } },
        { FilterOperator.Contains, new[] { typeof(string) } },
        { FilterOperator.GreaterThan, new[] { typeof(int), typeof(DateTime) /* diğer sayısal türler */ } },
        // Diğer operatörler ve geçerli türler...
    };
    public static readonly FrozenDictionary<Type, List<FilterOperator>> TypeBasedOperators;
    public static readonly FrozenDictionary<FilterOperator, string> OperatorHumanReadable = new Dictionary<FilterOperator,string>
    {
                { FilterOperator.Equals, "Eşittir" },
                { FilterOperator.Contains, "İçerir" },
                { FilterOperator.GreaterThan, "Büyüktür" },
                { FilterOperator.GreaterThanOrEqual, "Büyüktür ve eşittir" },
                { FilterOperator.LessThan, "Küçüktür" },
                { FilterOperator.LessThanOrEqual, "Küçüktür ve eşittir" },
                { FilterOperator.NotEquals, "Eşit değildir" },
                { FilterOperator.StartsWith, "İle başlar" },
                { FilterOperator.EndsWith, "İle biter" }
                // Diğer operatörler...
     }.ToFrozenDictionary();
    static OperatorTypeStorage()
    {
        TypeBasedOperators = OperatorTypes
            .SelectMany(kv => kv.Value, (kv, type) => new { type, kv.Key })
            .GroupBy(x => x.type)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Key).ToList()).ToFrozenDictionary();
    }
}
