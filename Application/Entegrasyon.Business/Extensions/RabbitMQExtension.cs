using Entegrasyon.Business.Utility.MessageBroker.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace Entegrasyon.Business.Extensions;

public static class RabbitMQExtension
{
    public static IServiceCollection AddRabbitMQ(this IServiceCollection services,IConfiguration configuration)
    {
        services.AddSingleton(sp =>
        {
            var factory = new ConnectionFactory()
            {
                HostName = configuration["RabbitMQ:HostName"],
                Port = int.TryParse(configuration["RabbitMQ:Port"],out var port) ? port : 5672,
                UserName = configuration["RabbitMQ:UserName"],
                Password = configuration["RabbitMQ:Password"],
            };
            return TryConnect(factory);
            return factory.CreateConnection();
        });

        services.AddSingleton<RabbitMQClientService>();
        services.AddSingleton<RabbitMqPublisherService>();
        return services;
    }

    private static IConnection TryConnect(ConnectionFactory factory)
    {
        int tryCount=0;
        while (true)
        {
            try
            {
                tryCount++;
                return factory.CreateConnection();
            }
            catch(Exception e)
            {
                Thread.Sleep(5000);
                if (tryCount == 5)
                    throw new Exception("RabbitMQ'ya bağlanamadı." + e.Message);
            }
        }
    }
}