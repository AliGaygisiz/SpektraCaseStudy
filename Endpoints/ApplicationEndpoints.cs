using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SpektraCaseStudy.Application.DTOs;
using SpektraCaseStudy.Application.Services;

namespace SpektraCaseStudy.Endpoints;

public static class ApplicationEndpoints
{
    public static void MapApplicationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/events").RequireAuthorization();

        group.MapPost(
            "/ingest",
            (
                [FromBody] IngestEventRequest model,
                IngestionService service,
                ClaimsPrincipal user,
                ILogger<IngestEventRequest> logger
            ) =>
            {
                var tenantId = user.FindFirstValue("TenantId");
                if (string.IsNullOrEmpty(tenantId))
                {
                    logger.LogWarning("Unauthorized ingest attempt: Missing TenantId");
                    return Results.Unauthorized();
                }

                if (!service.Ingest(tenantId, model))
                {
                    logger.LogInformation(
                        "Ingest accepted but skipped (Duplicate/Old): {UserId} {EventName}",
                        model.User_id,
                        model.Event_name
                    );
                    return Results.Accepted();
                }

                logger.LogInformation(
                    "Successfully ingested: {UserId} {EventName}",
                    model.User_id,
                    model.Event_name
                );
                return Results.Accepted();
            }
        );

        group.MapGet(
            "/aggregate/{userId}/{eventName}",
            async (
                string userId,
                string eventName,
                EvaluationService evaluationService,
                ClaimsPrincipal user
            ) =>
            {
                var tenantId = user.FindFirstValue("TenantId");
                if (string.IsNullOrEmpty(tenantId))
                    return Results.Unauthorized();

                var record = await evaluationService.GetAggregateAsync(tenantId, userId, eventName);

                return Results.Ok(record);
            }
        );

        group.MapPost(
            "/evaluate",
            async (
                [FromBody] EvaluateRequest request,
                EvaluationService evaluationService,
                ClaimsPrincipal user
            ) =>
            {
                var tenantId = user.FindFirstValue("TenantId");
                if (string.IsNullOrEmpty(tenantId))
                    return Results.Unauthorized();

                try
                {
                    var result = await evaluationService.EvaluateAsync(tenantId, request);
                    return Results.Ok(result);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(ex.Message);
                }
            }
        );
    }
}
