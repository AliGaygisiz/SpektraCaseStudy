using SpektraCaseStudy.Domain.Entities;

namespace SpektraCaseStudy.Application.DTOs;

public readonly struct AggregateDto
{
    public string TenantId { get; }
    public string UserId { get; }
    public string EventName { get; }
    public double SumValue { get; }
    public double MinValue { get; }
    public double MaxValue { get; }
    public long FirstTs { get; }
    public double FirstValue { get; }
    public long LastTs { get; }
    public double LastValue { get; }

    public AggregateDto(
        string tenantId, string userId, string eventName,
        double sumValue, double minValue, double maxValue,
        long firstTs, double firstValue,
        long lastTs, double lastValue)
    {
        TenantId = tenantId;
        UserId = userId;
        EventName = eventName;
        SumValue = sumValue;
        MinValue = minValue;
        MaxValue = maxValue;
        FirstTs = firstTs;
        FirstValue = firstValue;
        LastTs = lastTs;
        LastValue = lastValue;
    }

    public static AggregateDto FromEntity(UserAggregate entity)
    {
        return new AggregateDto(
            entity.TenantId, entity.UserId, entity.EventName,
            entity.SumValue, entity.MinValue, entity.MaxValue,
            entity.FirstTs, entity.FirstValue,
            entity.LastTs, entity.LastValue
        );
    }

    public static AggregateDto Default(string tenantId, string userId, string eventName)
    {
        return new AggregateDto(
            tenantId, userId, eventName,
            0, 0, 0,
            0, 0,
            0, 0
        );
    }
}
