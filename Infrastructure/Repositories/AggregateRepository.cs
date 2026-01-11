using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SpektraCaseStudy.Application.Interfaces;
using SpektraCaseStudy.Domain.Entities;
using SpektraCaseStudy.Infrastructure.Persistence;

namespace SpektraCaseStudy.Infrastructure.Repositories;

public class AggregateRepository : IAggregateRepository
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AggregateRepository> _logger;

    public AggregateRepository(
        IServiceProvider serviceProvider,
        ILogger<AggregateRepository> logger
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<UserAggregate?> GetAsync(string tenantId, string userId, string eventName)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await context.UserAggregates.FirstOrDefaultAsync(a =>
            a.TenantId == tenantId && a.UserId == userId && a.EventName == eventName
        );
    }

    public async Task BulkUpsertAsync(List<UserAggregate> markedRecords)
    {
        if (markedRecords.Count == 0)
            return;

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var connection = (NpgsqlConnection)context.Database.GetDbConnection();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = (NpgsqlTransaction)transaction.GetDbTransaction();
                    cmd.CommandText =
                        "CREATE TEMP TABLE IF NOT EXISTS temp_updates (LIKE \"identity\".\"UserAggregates\") ON COMMIT DROP;";
                    await cmd.ExecuteNonQueryAsync();
                }

                using (
                    var writer = await connection.BeginBinaryImportAsync(
                        "COPY temp_updates (\"TenantId\", \"UserId\", \"EventName\", \"SumValue\", \"MinValue\", \"MaxValue\", \"FirstTs\", \"FirstValue\", \"LastTs\", \"LastValue\") FROM STDIN (FORMAT BINARY)"
                    )
                )
                {
                    foreach (var r in markedRecords)
                    {
                        await writer.StartRowAsync();
                        await writer.WriteAsync(r.TenantId);
                        await writer.WriteAsync(r.UserId);
                        await writer.WriteAsync(r.EventName);
                        await writer.WriteAsync(r.SumValue);
                        await writer.WriteAsync(r.MinValue);
                        await writer.WriteAsync(r.MaxValue);
                        await writer.WriteAsync(r.FirstTs);
                        await writer.WriteAsync(r.FirstValue);
                        await writer.WriteAsync(r.LastTs);
                        await writer.WriteAsync(r.LastValue);
                    }
                    await writer.CompleteAsync();
                }

                using (var mergeCmd = connection.CreateCommand())
                {
                    mergeCmd.Transaction = (NpgsqlTransaction)transaction.GetDbTransaction();
                    mergeCmd.CommandText = """
                    INSERT INTO "identity"."UserAggregates" AS t
                    ("TenantId", "UserId", "EventName", "SumValue", "MinValue", "MaxValue", "FirstTs", "FirstValue", "LastTs", "LastValue")
                    SELECT "TenantId", "UserId", "EventName", "SumValue", "MinValue", "MaxValue", "FirstTs", "FirstValue", "LastTs", "LastValue"
                    FROM temp_updates
                    ON CONFLICT ("TenantId", "UserId", "EventName") 
                    DO UPDATE SET
                        "SumValue" = EXCLUDED."SumValue",
                        "MinValue" = EXCLUDED."MinValue",
                        "MaxValue" = EXCLUDED."MaxValue",
                        "FirstTs" = EXCLUDED."FirstTs",
                        "FirstValue" = EXCLUDED."FirstValue",
                        "LastTs" = EXCLUDED."LastTs",
                        "LastValue" = EXCLUDED."LastValue";
                    """;
                    await mergeCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                _logger.LogInformation("Flushed {Count} records to DB", markedRecords.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during DB flush");
                throw;
            }
        });
    }
}
