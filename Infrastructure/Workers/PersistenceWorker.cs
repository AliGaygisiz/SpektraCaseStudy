using Microsoft.Extensions.Options;
using SpektraCaseStudy.Application.Interfaces;
using SpektraCaseStudy.Infrastructure.Configuration;

namespace SpektraCaseStudy.Infrastructure.Workers;

public class PersistenceWorker : BackgroundService
{
    private readonly IHotStorage _hotStorage;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PersistenceWorker> _logger;
    private readonly WorkerSettings _settings;

    public PersistenceWorker(
        IHotStorage hotStorage, 
        IServiceScopeFactory scopeFactory,
        ILogger<PersistenceWorker> logger,
        IOptions<WorkerSettings> settings)
    {
        _hotStorage = hotStorage;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PersistenceWorker starting with FlushInterval: {Flush}s, CleanupInterval: {Cleanup}m", 
            _settings.FlushIntervalSeconds, _settings.CleanupIntervalMinutes);

        using var flushTimer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.FlushIntervalSeconds));
        using var cleanupTimer = new PeriodicTimer(TimeSpan.FromMinutes(_settings.CleanupIntervalMinutes));

        var flushTask = Task.Run(async () =>
        {
            while (await flushTimer.WaitForNextTickAsync(stoppingToken))
            {
                await FlushToDatabase();
            }
        }, stoppingToken);

        var cleanupTask = Task.Run(async () =>
        {
            while (await cleanupTimer.WaitForNextTickAsync(stoppingToken))
            {
                PerformCleanup();
            }
        }, stoppingToken);

        await Task.WhenAll(flushTask, cleanupTask);
    }

    private async Task FlushToDatabase()
    {
        var markedRecords = _hotStorage.PopMarked();
        if (markedRecords.Count == 0) return;

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAggregateRepository>();

        try
        {
            await repository.BulkUpsertAsync(markedRecords);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during DB flush");
        }
    }

    private void PerformCleanup()
    {
        _logger.LogInformation("Starting periodic cleanup (Threshold: {Threshold}m)", _settings.ColdEventThresholdMinutes);
        _hotStorage.CleanupProcessedEvents();
        _hotStorage.CleanupColdEvents(_settings.ColdEventThresholdMinutes);
    }
}
