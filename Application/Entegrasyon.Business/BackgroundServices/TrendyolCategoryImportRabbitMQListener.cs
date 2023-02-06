using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Utility.MessageBroker;
using Entegrasyon.Business.Utility.MessageBroker.RabbitMQ;
using Entegrasyon.Entity.Dtos.Category.Import.TrendyolImport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Entegrasyon.Business.BackgroundServices;

public class TrendyolCategoryImportRabbitMQListener:BackgroundService
{
    private IModel _model;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TrendyolCategoryImportRabbitMQListener> _logger;
    private readonly RabbitMQClientService _clientService;
    public TrendyolCategoryImportRabbitMQListener(IServiceProvider serviceProvider, ILogger<TrendyolCategoryImportRabbitMQListener> logger, RabbitMQClientService clientService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _clientService = clientService;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Trendyol Kategori Aktarım işlem kaydı başladı.");
        _model = _clientService.Connect();
        _model.BasicQos(0,1,false);
        //_model.QueueDeclare(MessageBrokerNames.TrendyolCategoryImportQueueName,true,false,false,null);
        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Trendyol Kategori Aktarım işlem kaydı çalıştırılıyor.");
        var consumer = new EventingBasicConsumer(_model);
        consumer.Received += Consumer_Received;
        _model.BasicConsume(MessageBrokerNames.TrendyolCategoryImportQueueName, false,consumer);
        return Task.CompletedTask;
    }

    private void Consumer_Received(object sender,BasicDeliverEventArgs @event)
    {
        var body = @event.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        List<TrendyolImport> categories = JsonConvert.DeserializeObject<IEnumerable<TrendyolImport>>(message)!.ToList();
        var serviceScope = _serviceProvider.CreateScope();
        var task = Task.Run(()=>serviceScope!.ServiceProvider.GetService<TrendyolCategoryImporterService>()!.Import(categories));
        task.Wait();
        if(task.Result.Success)
            _model.BasicAck(@event.DeliveryTag,false);
        else
            _logger.LogWarning("Rabbitmq trendyol hatası : "+task.Result.Message);
        serviceScope.Dispose();
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogWarning("Trendyol Kategori Aktarım işlem kaydı bitti.");
        return base.StopAsync(cancellationToken);
    }
}