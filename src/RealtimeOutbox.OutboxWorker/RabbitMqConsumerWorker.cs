using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace RealtimeOutbox.OutboxWorker;

public class RabbitMqConsumerWorker : BackgroundService
{
    private readonly ILogger<RabbitMqConsumerWorker> _logger;

    public RabbitMqConsumerWorker(ILogger<RabbitMqConsumerWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RabbitMQ Consumer Worker running at: {time}", DateTimeOffset.Now);

        while(!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}

