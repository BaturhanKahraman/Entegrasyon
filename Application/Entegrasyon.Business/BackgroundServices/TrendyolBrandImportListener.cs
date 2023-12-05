using Entegrasyon.Business.Concrete.Trendyol.Import;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Utility.MessageBroker.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Entegrasyon.Business.BackgroundServices;

public class TrendyolBrandImportListener : BackgroundService
{
    private IModel _model;
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMQClientService _clientService;
    private readonly ILogger<TrendyolBrandImportListener> _logger;
    public TrendyolBrandImportListener(IServiceProvider serviceProvider, RabbitMQClientService clientService, ILogger<TrendyolBrandImportListener> logger)
    {
        _serviceProvider = serviceProvider;
        _clientService = clientService;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Trendyol Marka Aktarım işlem kaydı başladı.");
        _model = _clientService.Connect();
        _model.BasicQos(0,1,false);
        //_model.QueueDeclare(MessageBrokerNames.TrendyolCategoryImportQueueName,true,false,false,null);
        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_model);
        consumer.Received += ConsumerOnReceived;
        _model.QueueDeclare(MessageBrokerNames.TrendyolBrandImportQueueName,true,false,false,null);
        _model.BasicConsume(MessageBrokerNames.TrendyolBrandImportQueueName,false,consumer);
        return Task.CompletedTask;
    }

    private void ConsumerOnReceived(object sender, BasicDeliverEventArgs e)
    {
        var serviceScope = _serviceProvider.CreateScope();
        var task = Task.Run(() => serviceScope!.ServiceProvider.GetService<TrendyolBrandImporterService>()!.ImportAll());
        task.Wait();
        if(task.Result.Success)
            _model.BasicAck(e.DeliveryTag,false);
        else
            _logger.LogWarning("Rabbitmq trendyol hatası : " + task.Result.Message);
        serviceScope.Dispose();
        throw new NotImplementedException();
    }
}