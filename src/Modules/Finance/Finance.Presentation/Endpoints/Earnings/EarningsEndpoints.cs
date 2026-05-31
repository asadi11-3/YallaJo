using Finance.Application.Earnings;
using Finance.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Finance.Presentation.Endpoints.Earnings;

internal static class EarningsEndpoints
{
    internal static void MapEarningsEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/guide", async (ICurrentUser currentUser, ISender sender, CancellationToken ct, [FromQuery] DateTime? fromUtc = null, [FromQuery] DateTime? toUtc = null) =>
        {
            var result = await sender.Send(new GetGuideEarningsQuery(currentUser.UserId!.Value, fromUtc, toUtc), ct);
            return result.ToApiResult();
        })
        .WithName("GetGuideEarnings")
        .WithSummary("List the current guide's earnings.")
        .WithTags("Finance | Earnings")
        .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.Payout, AppAction.Read))
        .RequireAuthorization();

        group.MapGet("/guide/summary", async (ICurrentUser currentUser, ISender sender, CancellationToken ct, [FromQuery] DateTime? fromUtc = null, [FromQuery] DateTime? toUtc = null) =>
        {
            var result = await sender.Send(new GetGuideEarningsSummaryQuery(currentUser.UserId!.Value, fromUtc, toUtc), ct);
            return result.ToApiResult();
        })
        .WithName("GetGuideEarningsSummaryWithDateRange")
        .WithSummary("Get the current guide's earnings summary for a date range.")
        .WithTags("Finance | Earnings")
        .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.Payout, AppAction.Read))
        .RequireAuthorization();

        group.MapGet("/admin/dashboard", async (ISender sender, CancellationToken ct, [FromQuery] DateTime? fromUtc = null, [FromQuery] DateTime? toUtc = null) =>
        {
            var result = await sender.Send(new GetAdminFinanceDashboardQuery(fromUtc, toUtc), ct);
            return result.ToApiResult();
        })
        .WithName("GetAdminFinanceDashboard")
        .WithSummary("Admin: finance dashboard overview.")
        .WithTags("Finance | Dashboard")
        .Produces<AdminFinanceDashboardDto>(StatusCodes.Status200OK)
        .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.AdminFinanceDashboard, AppAction.Read))
        .RequireAuthorization();
    }
}
