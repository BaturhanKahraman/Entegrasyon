using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Trendyol.Import;
using Entegrasyon.MessageQueue.Commands.Trendyol.Import;

namespace Entegrasyon.MessageQueue.Handler.Trendyol.Import;

public class TrendyolCategoryImportedCommandHandler(ITrendyolCategoryImportService service)
{
    public async Task Handle(TrendyolCategoryImportCommand categories)
    {
        await Task.Delay(10000);
        await service.Import(categories.Categories);
    }
}
