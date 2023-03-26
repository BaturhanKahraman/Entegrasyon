using System.Text;
using Newtonsoft.Json;

namespace Entegrasyon.Business.Utility.MessageBroker.RabbitMQ;

public class RabbitMqPublisherService
{
    private readonly RabbitMQClientService _clientService;

    public RabbitMqPublisherService(RabbitMQClientService clientService)
    {
        _clientService = clientService;
    }

    public void PublishToQueue(string queueName, object data)
    {
        var model = _clientService.Connect();
        var message = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
        var properties = model.CreateBasicProperties();
        properties.Persistent = true;
        model.QueueDeclare(queueName,true,false,false,null);
        model.BasicPublish(string.Empty,queueName,false,properties,message);
        model.ConfirmSelect();
    }
    public void PublishToExchange<T>(string exchangeName,string queueName,T data)
    {
        var model = _clientService.Connect();
        var message = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
        var properties = model.CreateBasicProperties();
        properties.Persistent = true;
        //model.ExchangeDeclare(exchangeName,ExchangeType.Direct,true,false,null);
        //model.
        //model.BasicPublish(string.Empty,queueName,false,properties,message);
        model.ConfirmSelect();
    }
}