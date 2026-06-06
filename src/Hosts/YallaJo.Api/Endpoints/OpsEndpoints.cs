using ContentTours.Application.Commands.Snapshot.TriggerTourSnapshotBackfill;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Outbox;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace YallaJo.Api.Endpoints;

/// <summary>
/// Operational endpoints for outbox dead-letter management.
/// All endpoints require Permission.Outbox.{Read|Replay}.
/// </summary>
public static class OpsEndpoints
{
    public static IEndpointRouteBuilder MapOpsEndpoints(this IEndpointRouteBuilder app)
    {
        var ops = app.MapGroup("/api/v1/ops/outbox")
            .WithTags("Operations — Outbox")
            .RequireAuthorization();

        ops.MapGet("/dead-letters",
            async (ISender sender, string? module, int limit, CancellationToken ct) =>
            {
                var result = await sender.Send(new ListDeadLettersQuery(module, limit > 0 ? limit : 50), ct);
                return result.ToApiResult();
            })
            .WithMetadata(new MustHavePermissionAttribute(OpsFeatures.Outbox, AppAction.Read))
            .WithName("ListOutboxDeadLetters")
            .WithSummary("List dead-lettered outbox messages across all modules")
            .Produces<ListDeadLettersResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        ops.MapPost("/dead-letters/{module}/{id:guid}/replay",
            async (string module, Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new ReplayDeadLetterCommand(module, id), ct);
                return result.ToApiResult();
            })
            .WithMetadata(new MustHavePermissionAttribute(OpsFeatures.Outbox, AppAction.Replay))
            .WithName("ReplayOutboxDeadLetter")
            .WithSummary("Replay a dead-lettered outbox message by cloning it with fresh retry state")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        var contentTours = app.MapGroup("/api/v1/ops/content-tours")
            .WithTags("Operations — ContentTours")
            .RequireAuthorization();

        contentTours.MapPost("/backfill/tour-snapshots",
            async (ISender sender, int? batchSize, CancellationToken ct) =>
            {
                var result = await sender.Send(new TriggerTourSnapshotBackfillCommand(batchSize ?? 100), ct);
                return result.ToApiResult();
            })
            .WithMetadata(new MustHavePermissionAttribute(OpsFeatures.Outbox, AppAction.Replay))
            .WithName("BackfillTourSnapshots")
            .WithSummary("Re-emit enriched TourApproved events for all approved tours to repopulate Booking snapshots")
            .Produces<TriggerTourSnapshotBackfillResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }
}
