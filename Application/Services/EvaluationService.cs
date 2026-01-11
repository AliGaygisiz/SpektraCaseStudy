using SpektraCaseStudy.Application.DTOs;
using SpektraCaseStudy.Application.Interfaces;
using SpektraCaseStudy.Domain.Entities;

namespace SpektraCaseStudy.Application.Services;

public class EvaluationService
{
    private readonly IHotStorage _hotStorage;
    private readonly IAggregateRepository _repository;

    public EvaluationService(IHotStorage hotStorage, IAggregateRepository repository)
    {
        _hotStorage = hotStorage;
        _repository = repository;
    }

    public async ValueTask<AggregateDto> GetAggregateAsync(
        string tenantId,
        string userId,
        string eventName
    )
    {
        var record = _hotStorage.Get(tenantId, userId, eventName);
        if (record != null)
        {
            return AggregateDto.FromEntity(record);
        }

        var storedRecord = await _repository.GetAsync(tenantId, userId, eventName);
        if (storedRecord != null)
        {
            return AggregateDto.FromEntity(storedRecord);
        }

        return AggregateDto.Default(tenantId, userId, eventName);
    }

    public async Task<bool> EvaluateAsync(string tenantId, EvaluateRequest request)
    {
        var parts = request.Expression.Split(' ');
        if (parts.Length != 3)
            throw new ArgumentException("Invalid expression format");

        var pathParts = parts[0].Split('.');
        if (pathParts.Length != 2)
            throw new ArgumentException("Invalid field format");

        var eventName = pathParts[0];
        var field = pathParts[1];
        var op = parts[1];
        if (!double.TryParse(parts[2], out var targetValue))
            throw new ArgumentException("Invalid number value");

        var aggregate = await GetAggregateAsync(tenantId, request.User_id, eventName);

        double actualValue = 0;
        switch (field.ToLower())
        {
            case "sum_value":
                actualValue = aggregate.SumValue;
                break;
            case "min_value":
                actualValue = aggregate.MinValue;
                break;
            case "max_value":
                actualValue = aggregate.MaxValue;
                break;
            case "first_ts":
                actualValue = aggregate.FirstTs;
                break;
            case "last_ts":
                actualValue = aggregate.LastTs;
                break;
            case "first_value":
                actualValue = aggregate.FirstValue;
                break;
            case "last_value":
                actualValue = aggregate.LastValue;
                break;
            default:
                throw new ArgumentException("Unknown field");
        }

        bool result = false;
        switch (op)
        {
            case ">":
                result = actualValue > targetValue;
                break;
            case ">=":
                result = actualValue >= targetValue;
                break;
            case "<":
                result = actualValue < targetValue;
                break;
            case "<=":
                result = actualValue <= targetValue;
                break;
            case "==":
                result = Math.Abs(actualValue - targetValue) < 0.0001;
                break;
            case "!=":
                result = Math.Abs(actualValue - targetValue) > 0.0001;
                break;
            default:
                throw new ArgumentException("Unknown operator");
        }

        return result;
    }
}
