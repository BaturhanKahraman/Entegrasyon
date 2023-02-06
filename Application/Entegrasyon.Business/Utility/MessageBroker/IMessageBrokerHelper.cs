namespace Entegrasyon.Business.Utility.MessageBroker;

public interface IMessageBrokerHelper
{
    void PublishQueue<T>(string queueName,T data);
    void PublishExchange<T>(string exchangeName,string routingKey,T data);
}