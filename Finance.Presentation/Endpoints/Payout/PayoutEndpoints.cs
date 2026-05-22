using Finance.Application.Commands.ApprovePayout;
using Finance.Application.Commands.TriggerPayout;
using Finance.Application.Queries.Dtos;
using Finance.Application.Queries.GetMyProviderPayouts;
using Finance.Application.Queries.GetPayoutById;
using Finance.Application.Queries.GetPendingPayouts;
using Finance.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Finance.Presentation.Endpoints.Payout;

internal static class PayoutEndpoints
{
    public static void MapPayoutEndpoints(RouteGroupBuilder group)
    {
        // GET /admin/pending — admin only
        group.MapGet("/admin/pending", async (
            ISender sender,
            CancellationToken ct,
            [FromQuery] Guid? cursor = null,
            [FromQuery] int pageSize = 20) =>
        {
            var result = await sender.Send(new GetPendingPayoutsQuery(cursor, pageSize), ct);
            return result.ToApiResult();
        })
        .WithName("GetPendingPayouts")
        .WithSummary("Admin: payouts awaiting approval (Pending/Hold).")
        .WithTags("Payouts")
        .Produces<PayoutPageDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.Payout, AppAction.Read))
        .RequireAuthorization();

        // GET /provider — provider self-view
        group.MapGet("/provider", async (
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct,
            [FromQuery] Guid? cursor = null,
            [FromQuery] int pageSize = 20) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            {
                return Result.Failure<PayoutPageDto>(
                    new Error("Payout.Unauthorized", "Authentication required."),
                    Outcome.Unauthorized).ToApiResult();
            }

            var providerId = TryParseProviderClaim(currentUser);
            if (providerId is null)
            {
                return Result.Failure<PayoutPageDto>(
                    new Error("Payout.OwnerMismatch", "Provider claim required."),
                    Outcome.Forbidden).ToApiResult();
            }

            var result = await sender.Send(
                new GetMyProviderPayoutsQuery(providerId.Value, cursor, pageSize), ct);
            return result.ToApiResult();
        })
        .WithName("GetMyProviderPayouts")
        .WithSummary("Provider self-view of own payouts.")
        .WithTags("Payouts")
        .Produces<PayoutPageDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.Payout, AppAction.Read))
        .RequireAuthorization();

        // GET /{id:guid}
        group.MapGet("/{id:guid}", async (
            Guid id,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            {
                return Result.Failure<PayoutDto>(
                    new Error("Payout.Unauthorized", "Authentication required."),
                    Outcome.Unauthorized).ToApiResult();
            }

            var isAdmin = currentUser.HasPermission("Permission.AdminFinanceDashboard.Read");
            var providerId = TryParseProviderClaim(currentUser);
            var result = await sender.Send(
                new GetPayoutByIdQuery(id, currentUser.UserId.Value, isAdmin, providerId), ct);
            return result.ToApiResult();
        })
        .WithName("GetPayoutById")
        .WithSummary("Get one payout by id (self/admin).")
        .WithTags("Payouts")
        .Produces<PayoutDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.Payout, AppAction.Read))
        .RequireAuthorization();

        // POST /admin/trigger — admin manually sweeps
        group.MapPost("/admin/trigger", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new TriggerPayoutCommand(IsManual: true), ct);
            return result.ToApiResult();
        })
        .WithName("TriggerPayout")
        .WithSummary("Admin: manually trigger a payout sweep cycle.")
        .WithTags("Payouts")
        .Produces<TriggerPayoutResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.Payout, AppAction.Trigger))
        .RequireAuthorization();

        // POST /{id}/approve — admin approves large payouts
        group.MapPost("/{id:guid}/approve", async (
            Guid id,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            {
                return Result.Failure<ApprovePayoutResult>(
                    new Error("Payout.Unauthorized", "Authentication required."),
                    Outcome.Unauthorized).ToApiResult();
            }

            var result = await sender.Send(new ApprovePayoutCommand(id, currentUser.UserId.Value), ct);
            return result.ToApiResult();
        })
        .WithName("ApprovePayout")
        .WithSummary("Admin: approve a Pending payout > LargePayoutThreshold.")
        .WithTags("Payouts")
        .Produces<ApprovePayoutResult>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.Payout, AppAction.Approve))
        .RequireAuthorization();
    }

    private static Guid? TryParseProviderClaim(ICurrentUser currentUser)
    {
        var raw = currentUser.GetClaim("provider_id");
        return Guid.TryParse(raw, out var pid) ? pid : null;
    }
}
