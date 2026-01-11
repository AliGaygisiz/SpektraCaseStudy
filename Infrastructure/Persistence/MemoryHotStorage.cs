using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using SpektraCaseStudy.Application.DTOs;
using SpektraCaseStudy.Application.Interfaces;
using SpektraCaseStudy.Domain.Entities;
using SpektraCaseStudy.Infrastructure.Configuration;

namespace SpektraCaseStudy.Infrastructure.Persistence;

public class MemoryHotStorage : IHotStorage
{
    private readonly ConcurrentDictionary<string, UserAggregate> _hotStorage = new();
    private readonly ConcurrentQueue<string> _markedKeys = new();
    private readonly ConcurrentDictionary<string, long> _processedEvents = new();
    private readonly ILogger<MemoryHotStorage> _logger;
    private readonly WorkerSettings _settings;

    public MemoryHotStorage(ILogger<MemoryHotStorage> logger, IOptions<WorkerSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public void AddOrUpdate(string tenantId, IngestEventRequest ev)
    {
        var key = $"{tenantId}:{ev.User_id}:{ev.Event_name}";
        _logger.LogInformation("Adding/Updating key: {Key}", key);

        _hotStorage.AddOrUpdate(
            key,
            _ =>
            {
                var record = new UserAggregate
                {
                    TenantId = tenantId,
                    UserId = ev.User_id,
                    EventName = ev.Event_name,
                    SumValue = ev.Value,
                    MinValue = ev.Value,
                    MaxValue = ev.Value,
                    FirstTs = ev.Ts,
                    FirstValue = ev.Value,
                    LastTs = ev.Ts,
                    LastValue = ev.Value,
                };
                _markedKeys.Enqueue(key);
                _logger.LogInformation("Added new record for {Key}", key);
                return record;
            },
            (_, existing) =>
            {
                lock (existing)
                {
                    if (ev.Ts > existing.LastTs)
                    {
                        existing.LastTs = ev.Ts;
                        existing.LastValue = ev.Value;
                    }
                    else if (ev.Ts < existing.FirstTs)
                    {
                        existing.FirstTs = ev.Ts;
                        existing.FirstValue = ev.Value;
                    }

                    if (ev.Value > existing.MaxValue)
                        existing.MaxValue = ev.Value;
                    else if (ev.Value < existing.MinValue)
                        existing.MinValue = ev.Value;

                    existing.SumValue += ev.Value;

                    _markedKeys.Enqueue(key);
                }
                _logger.LogInformation("Updated existing record for {Key}", key);
                return existing;
            }
        );
    }

    public UserAggregate? Get(string tenantId, string userId, string eventName)
    {
        var key = $"{tenantId}:{userId}:{eventName}";
        if (_hotStorage.TryGetValue(key, out var record))
        {
            return record;
        }
        return null;
    }

    public List<UserAggregate> PopMarked()
    {
        var updates = new Dictionary<string, UserAggregate>();
        while (_markedKeys.TryDequeue(out var key))
        {
            if (_hotStorage.TryGetValue(key, out var record))
            {
                updates[key] = record;
            }
        }
        return [.. updates.Values];
    }

    public bool IsDuplicateOrTooOld(IngestEventRequest ev)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var threshold = _settings.DuplicateEventThresholdSeconds;

        if (ev.Ts < (now - threshold))
            return true;

        var expiry = now + threshold;
        return !_processedEvents.TryAdd(ev.Event_id, expiry);
    }

    public void CleanupProcessedEvents()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (var entry in _processedEvents)
        {
            if (entry.Value < now)
                _processedEvents.TryRemove(entry.Key, out _);
        }
    }

    public void CleanupColdEvents(int thresholdMinutes)
    {
        var threshold = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - (thresholdMinutes * 60);

        foreach (var entry in _hotStorage)
        {
            if (entry.Value.LastTs < threshold)
            {
                _hotStorage.TryRemove(entry.Key, out _);
            }
        }
    }
}
