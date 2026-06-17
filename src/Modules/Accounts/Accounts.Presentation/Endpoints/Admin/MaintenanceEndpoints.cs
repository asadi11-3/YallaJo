using Accounts.Application.Commands.Maintenance.BackfillProviderDocuments;
using Accounts.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Accounts.Presentation.Endpoints.Admin;

/// <summary>
/// Admin-only maintenance endpoints (Patch 2B+). Dry-run is ALWAYS the default;
/// writes require an explicit <c>?apply=true</c> or <c>?dryRun=false</c> on the
/// request line. Permission gate: <c>Permission.AdminProviderQueue.Update</c>.
/// </summary>
public static class MaintenanceEndpoints
{
    public static IEndpointRouteBuilder MapMaintenanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints;

        // POST /api/v1/admin/maintenance/backfill-provider-documents
        //   ?apply=true|false   (default: false  -> dry-run)
        //   ?dryRun=true|false  (default unset; overridden by apply when present)
        //   ?maxRows=500        (clamped to [1, 5000])
        //   ?afterId={guid}     (keyset cursor for resumable runs)
        group.MapPost("/backfill-provider-documents", async (
            [FromQuery] bool? apply,
            [FromQuery] bool? dryRun,
            [FromQuery] int? maxRows,
            [FromQuery] Guid? afterId,
            ISender sender,
            CancellationToken ct) =>
        {
            // Dry-run defaults ON. apply=true OR dryRun=false explicitly enables writing.
            bool effectiveDryRun = dryRun ?? !(apply ?? false);
            int rows = maxRows ?? 0; // handler clamps to default/cap.

            var result = await sender.Send(
                new BackfillProviderDocumentsCommand(rows, effectiveDryRun, afterId),
                ct);

            return result.ToApiResult();
        })
        .WithName("BackfillProviderDocuments")
        .Produces<BackfillProviderDocumentsReport>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithSummary(
            "Backfill FileAssets + ProviderDocumentFiles from existing ProviderDocuments.FileUrl. "
            + "Dry-run default ON; pass apply=true or dryRun=false to commit.")
        .WithMetadata(new MustHavePermissionAttribute(
            AccountsFeatures.AdminProviderQueue, AppAction.Update))
        .RequireAuthorization();

        return endpoints;
    }
}
