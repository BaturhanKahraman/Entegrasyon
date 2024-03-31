using Entegrasyon.MqContracts.CategoryImport;
using MassTransit;
using Entegrasyon.Business.Concrete.Trendyol.Import;
namespace Entegrasyon.MqConsumer.CategoryImport;

public class CategoryImportConsumer(TrendyolCategoryImporterService service) : IConsumer<CategoryImported>
{
    public async Task Consume(ConsumeContext<CategoryImported> context)
    {
        await service.Import(context.Message.Categories);
    }
}