using Entegrasyon.Business.Utility.Constants;
using RabbitMQ.Client;

namespace Entegrasyon.Business.Utility.MessageBroker.RabbitMQ;

public class RabbitMQClientService:IDisposable
{
    private IModel _model;
    private readonly IConnection _connection;

    public RabbitMQClientService(IConnection connection)
    {
        _connection = connection;
    }

    public IModel Connect()
    {
        if(_model is {IsOpen:true})
            return _model;
        _model = _connection.CreateModel();

        _model.QueueDeclare(MessageBrokerNames.TrendyolCategoryImportQueueName,true,false,false,null);
        return _model;
    }

    public void Dispose()
    {
        _model?.Close();
        _model?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}