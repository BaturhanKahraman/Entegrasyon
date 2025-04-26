using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;
using System.Collections.Immutable;

namespace Entegrasyon.MessageQueue.Commands.Trendyol.Import;

public sealed record TrendyolCategoryImportCommand(ImmutableList<TrendyolSelectedCategory> Categories);
