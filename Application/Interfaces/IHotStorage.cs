using SpektraCaseStudy.Application.DTOs;
using SpektraCaseStudy.Domain.Entities;

namespace SpektraCaseStudy.Application.Interfaces;

public interface IHotStorage
{
    void AddOrUpdate(string tenantId, IngestEventRequest ev);
    UserAggregate? Get(string tenantId, string userId, string eventName);
    List<UserAggregate> PopMarked();

    bool IsDuplicateOrTooOld(IngestEventRequest ev);
    void CleanupProcessedEvents();
    void CleanupColdEvents(int thresholdMinutes);
}
