using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VMS.Core.Entities;
using VMS.Infrastructure.Data;
using VMS.Infrastructure.Repositories;

namespace VMS.Infrastructure.Services;

public class NotificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationBackgroundService> _logger;
    private readonly NotificationEventQueue _eventQueue;

    private readonly int _pollingIntervalSeconds;
    private readonly int _batchSize;
    private readonly int _presenceSyncIntervalSeconds;

    public NotificationBackgroundService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<NotificationBackgroundService> logger,
        NotificationEventQueue eventQueue)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
        _eventQueue = eventQueue;
        _pollingIntervalSeconds = _configuration.GetValue<int>("NotificationSettings:PollingIntervalSeconds", 5);
        _batchSize = _configuration.GetValue<int>("NotificationSettings:BatchSize", 50);
        _presenceSyncIntervalSeconds = _configuration.GetValue<int>("NotificationSettings:PresenceSyncIntervalSeconds", 30);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Background Service started");

        // Start presence sync task
        var presenceSyncTask = Task.Run(async () => await SyncPresencePeriodically(stoppingToken), stoppingToken);

        // Start processing task
        var processingTask = Task.Run(async () => await ProcessNotifications(stoppingToken), stoppingToken);

        await Task.WhenAll(presenceSyncTask, processingTask);
    }

    private async Task ProcessNotifications(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Process in-memory queue first (immediate)
                await ProcessInMemoryQueue(stoppingToken);

                // Then poll database for unprocessed events (fallback/recovery)
                await ProcessDatabaseEvents(stoppingToken);

                // Wait before next iteration
                await Task.Delay(TimeSpan.FromSeconds(_pollingIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification processing loop");
                await Task.Delay(TimeSpan.FromSeconds(_pollingIntervalSeconds), stoppingToken);
            }
        }
    }

    private async Task ProcessInMemoryQueue(CancellationToken stoppingToken)
    {
        try
        {
            var processedCount = 0;
            await foreach (var notificationEvent in _eventQueue.ReadAllAsync(stoppingToken))
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                using var scope = _serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<INotificationProcessor>();
                await processor.ProcessNotificationEventAsync(notificationEvent, stoppingToken);
                processedCount++;

                // Limit batch size
                if (processedCount >= _batchSize)
                    break;
            }

            if (processedCount > 0)
            {
                _logger.LogDebug("Processed {Count} events from in-memory queue", processedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing in-memory queue");
        }
    }

    private async Task ProcessDatabaseEvents(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<VmsDbContext>();
            var processor = scope.ServiceProvider.GetRequiredService<INotificationProcessor>();

            // Fetch unprocessed events in batches
            var unprocessedEvents = await context.NotificationEvents
                .Where(e => !e.Processed)
                .OrderBy(e => e.CreatedAt)
                .Take(_batchSize)
                .ToListAsync(stoppingToken);

            if (unprocessedEvents.Count > 0)
            {
                _logger.LogDebug("Processing {Count} events from database", unprocessedEvents.Count);

                foreach (var notificationEvent in unprocessedEvents)
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;

                    try
                    {
                        await processor.ProcessNotificationEventAsync(notificationEvent, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing notification event {EventId}", notificationEvent.Id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing database events");
        }
    }

    private async Task SyncPresencePeriodically(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_presenceSyncIntervalSeconds), stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var presenceService = scope.ServiceProvider.GetRequiredService<IPresenceService>();
                
                await presenceService.CleanupStaleConnectionsAsync(stoppingToken);
                await presenceService.SyncToDatabaseAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing presence");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Notification Background Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}

