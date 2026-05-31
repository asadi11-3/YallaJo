using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Social.Application.Commands.ResolveReport;
using Social.Application.Commands.SubmitReport;
using Social.Application.Queries.GetAdminReports;
using Social.Contracts.Authorization;
using Social.Presentation.Endpoints.Report.Models;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Social.Presentation.Endpoints.Report;

internal static class ReportEndpoints
{
    internal static void MapReportEndpoints(RouteGroupBuilder group)
    {
        group.WithTags("Social | Reports");

        // POST /api/v1/social/reports — submit a report
        group.MapPost("/", async (
            SubmitReportRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new SubmitReportCommand(
                    currentUser.UserId!.Value,
                    request.EntityType,
                    request.EntityId,
                    request.Reason,
                    request.Description), ct);

            return result.ToApiResult();
        })
        .WithName("SubmitReport")
        .WithSummary("Submit a report")
        .WithDescription("Reports an entity (Review, Tour, Place, Business, or Blog) for moderation.")
        .Accepts<SubmitReportRequest>("application/json")
        .Produces<Guid>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.Report, AppAction.Create))
        .RequireAuthorization();

        // GET /api/v1/social/reports/admin — admin list
        group.MapGet("/admin", async (
            [Microsoft.AspNetCore.Mvc.FromQuery] Guid? afterCursor,
            [Microsoft.AspNetCore.Mvc.FromQuery] int pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetAdminReportsQuery(afterCursor, pageSize == 0 ? 20 : pageSize), ct);

            return result.ToApiResult();
        })
        .WithName("GetAdminReports")
        .WithSummary("Admin: list reports")
        .WithDescription("Returns a cursor-paginated list of all submitted reports (admin-only).")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.AdminModerationQueue, AppAction.Read))
        .RequireAuthorization();

        // POST /api/v1/social/reports/admin/{id}/resolve — admin resolves a report
        group.MapPost("/admin/{id:guid}/resolve", async (
            Guid id,
            ResolveReportRequest request,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new ResolveReportCommand(currentUser.UserId!.Value, id, request.Action, request.Notes), ct);

            return result.ToApiResult();
        })
        .WithName("ResolveReport")
        .WithSummary("Admin: resolve a report")
        .WithDescription("Resolves an open report and optionally takes action on the underlying entity.")
        .Accepts<ResolveReportRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(SocialFeatures.AdminModerationQueue, AppAction.Resolve))
        .RequireAuthorization();
    }
}
