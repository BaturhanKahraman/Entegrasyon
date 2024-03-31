using Entegrasyon.MqContracts.Product;
using MassTransit;

namespace Entegrasyon.MqConsumer.Product;

public class ProductUpdatedConsumer:IConsumer<ProductUpdated>
{
    public Task Consume(ConsumeContext<ProductUpdated> context)
    {
        throw new NotImplementedException();
    }
}