using SpektraCaseStudy.Application.DTOs;
using SpektraCaseStudy.Application.Interfaces;

namespace SpektraCaseStudy.Application.Services;

public class IngestionService
{
    private readonly IHotStorage _hotStorage;
    private readonly ILogger<IngestionService> _logger;

    public IngestionService(IHotStorage hotStorage, ILogger<IngestionService> logger)
    {
        _hotStorage = hotStorage;
        _logger = logger;
    }

    public bool Ingest(string tenantId, IngestEventRequest request)
    {
        if (_hotStorage.IsDuplicateOrTooOld(request))
        {
            _logger.LogWarning("Event skipped (Duplicate or Too Old): {TenantId}:{UserId}:{EventName}", tenantId, request.User_id, request.Event_name);
            return false;
        }

        _logger.LogInformation("Ingesting event: {TenantId}:{UserId}:{EventName}", tenantId, request.User_id, request.Event_name);
        _hotStorage.AddOrUpdate(tenantId, request);
        return true;
    }
}
