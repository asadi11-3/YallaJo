using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Security.Application.Queries.Dtos;
using Security.Application.Queries.GetAuditLogs;
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Presentation;

namespace Security.Presentation.Endpoints.AuditLog;

internal static class AuditLogEndpoints
{
    internal static void MapAuditLogEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/audit-logs", async (ISender sender, CancellationToken ct, int page = 1, int pageSize = 20, Guid? userId = null) =>
        {
            var result = await sender.Send(new GetAuditLogsQuery(page, pageSize, userId), ct);
            return result.ToApiResult();
        })
        .WithName("GetAuditLogs")
        .Produces<PaginatedResult<AuditLogDto>>(StatusCodes.Status200OK)
        .WithSummary("Get paginated audit logs, optionally filtered by user")
        .WithMetadata(new MustHavePermissionAttribute(AppFeatures.System, AppAction.Read))
        .RequireAuthorization();
    }
}
