using System.Text;
using System.Text.Json;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Entity;
using RabbitMQ.Client;

namespace Entegrasyon.Business.Utility.MessageBroker.RabbitMQ;

public class RabbitMQHelper:IMessageBrokerHelper
{
    private readonly IConnection _connection;
    private readonly ApplicationUserManager _applicationUserService;

    public RabbitMQHelper(IConnection connection, ApplicationUserManager applicationUserService)
    {
        _connection = connection;
        _applicationUserService = applicationUserService;
    }

    public void PublishQueue<T>(string queueName,T data)
    {
        using var channel = _connection.CreateModel();
        channel.QueueDeclare(queueName,true,false,false,null);
        //var properties=channel.CreateBasicProperties();
        //properties.UserId = _applicationUserService.GetActiveUserId();
        //properties.Headers.Add("tenant-id",60000);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data));
        channel.BasicPublish(string.Empty,queueName,null,body);
        channel.ConfirmSelect();
    }

    public void PublishExchange<T>(string exchangeName,string routingKey,T data)
    {
        using var channel = _connection.CreateModel();
        channel.ExchangeDeclare(exchangeName,ExchangeType.Direct,true,false,null);
        var properties = channel.CreateBasicProperties();
        properties.UserId = _applicationUserService.GetActiveUserId();
        //properties.Headers.Add("tenant-id",60000);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data));
        channel.BasicPublish(exchangeName,routingKey,properties,body);
    }
    //public T ConsumeMessageQueue<T>(
}