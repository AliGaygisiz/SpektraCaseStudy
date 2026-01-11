using SpektraCaseStudy.Domain.Entities;

namespace SpektraCaseStudy.Application.Interfaces;

public interface IAggregateRepository
{
    Task<UserAggregate?> GetAsync(string tenantId, string userId, string eventName);
    Task BulkUpsertAsync(List<UserAggregate> aggregates);
}
