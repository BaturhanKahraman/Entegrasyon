using Entegrasyon.MqContracts.Product;
using MassTransit;

namespace Entegrasyon.MqConsumer.Product;

public class ProductSoldConsumer:IConsumer<ProductSold>
{
    public async Task Consume(ConsumeContext<ProductSold> context)
    {
        await Task.Yield();
    }
}