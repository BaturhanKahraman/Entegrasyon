using System.Collections.Immutable;
using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;

namespace Entegrasyon.MqContracts.CategoryImport;

public record CategoryImported(ImmutableList<TrendyolSelectedCategory> Categories);