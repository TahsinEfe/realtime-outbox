using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RealtimeOutbox.OutboxWorker.Data;
using RealtimeOutbox.OutboxWorker.Entities;
using RealtimeOutbox.OutboxWorker.Messaging;

namespace RealtimeOutbox.OutboxWorker;

public sealed class OutboxPublisherWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRabbitMqPublisher _publisher;
    private readonly ILogger<OutboxPublisherWorker> _logger;

    private const int BatchSize = 50;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int MaxAttempts = 10;

    public OutboxPublisherWorker(
        IServiceScopeFactory scopeFactory,
        IRabbitMqPublisher publisher,
        ILogger<OutboxPublisherWorker> logger)
        {
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _logger = logger;
        }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker started. Monitoring outbox_events table...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processedCount = await ProcessBatchAsync(stoppingToken);

                if (processedCount == 0)
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in the outbox polling loop.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Worker stopped.");
    }

    private async Task<int> ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        try
        {
            var sql = @"
                SELECT *
                FROM outbox_events
                WHERE ""SentAtUtc"" IS NULL
                  AND ""AttemptCount"" < {0}
                ORDER BY ""OccurredAtUtc""
                LIMIT {1}
                FOR UPDATE SKIP LOCKED";

            var batch = await db.OutboxEvents
                .FromSqlRaw(sql, MaxAttempts, BatchSize)
                .ToListAsync(ct);

            if (batch.Count == 0)
            {
                await tx.RollbackAsync(ct); 
                return 0;
            }

            _logger.LogInformation("Processing {Count} outbox events.", batch.Count);

            foreach (var ev in batch)
            {
                try
                {
                    await _publisher.PublishAsync(ev.Type, ev.Payload, ct);

                    ev.SentAtUtc = DateTimeOffset.UtcNow;
                    ev.LastError = null;
                }
                catch (Exception ex)
                {
                    ev.AttemptCount += 1;
                    ev.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;

                    _logger.LogWarning(ex, "Failed to publish outbox event {EventId}. Attempt: {Attempt}", ev.Id, ev.AttemptCount);
                }
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return batch.Count;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Batch processing failed, transaction rolled back.");
            throw;
        }
    }
}
