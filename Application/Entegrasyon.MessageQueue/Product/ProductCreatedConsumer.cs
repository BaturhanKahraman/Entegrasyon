using Entegrasyon.MqConsumer.Product;
using Entegrasyon.MqContracts.Product;
using MassTransit;

namespace Entegrasyon.MqConsumer.Product;

public class ProductCreatedConsumer : IConsumer<ProductCreated>
{
    public async Task Consume(ConsumeContext<ProductCreated> context)
    {
        await Task.Yield();
        //TODO
        //yapılacak işlemler.
    }
}