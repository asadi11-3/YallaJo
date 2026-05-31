using Finance.Application.Disputes;
using Finance.Contracts.Authorization;
using Finance.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Finance.Presentation.Endpoints.Dispute;

internal static class DisputeEndpoints
{
    internal static void MapDisputeEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/my", async (ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyDisputesQuery(currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        })
        .WithName("GetMyDisputes")
        .WithSummary("List the current user's disputes.")
        .WithTags("Finance | Disputes")
        .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.Refund, AppAction.Read))
        .RequireAuthorization();

        group.MapGet("/admin/open", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetOpenDisputesQuery(), ct);
            return result.ToApiResult();
        })
        .WithName("GetOpenDisputes")
        .WithSummary("Admin: list all open disputes.")
        .WithTags("Finance | Disputes")
        .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.AdminFinanceDashboard, AppAction.Read))
        .RequireAuthorization();

        group.MapPost("/", async (OpenDisputeRequest request, ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new OpenDisputeCommand(request.PaymentId, currentUser.UserId!.Value, request.Reason, request.Description), ct);
            return result.ToApiResult();
        })
        .WithName("OpenDispute")
        .WithSummary("Open a dispute against a payment.")
        .WithTags("Finance | Disputes")
        .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.Refund, AppAction.Create))
        .RequireAuthorization();

        group.MapPost("/{id:guid}/review", async (Guid id, ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new MarkDisputeUnderReviewCommand(id, currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        })
        .WithName("MarkDisputeUnderReview")
        .WithSummary("Admin: mark a dispute as under review.")
        .WithTags("Finance | Disputes")
        .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.AdminFinanceDashboard, AppAction.Update))
        .RequireAuthorization();

        group.MapPost("/{id:guid}/resolve", async (Guid id, ResolveDisputeRequest request, ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ResolveDisputeCommand(id, currentUser.UserId!.Value, request.Resolution, request.Notes), ct);
            return result.ToApiResult();
        })
        .WithName("ResolveDispute")
        .WithSummary("Admin: resolve a dispute.")
        .WithTags("Finance | Disputes")
        .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.AdminFinanceDashboard, AppAction.Approve))
        .RequireAuthorization();

        group.MapPost("/{id:guid}/escalate", async (Guid id, EscalateDisputeRequest request, ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new EscalateDisputeCommand(id, currentUser.UserId!.Value, request.Reason), ct);
            return result.ToApiResult();
        })
        .WithName("EscalateDispute")
        .WithSummary("Admin: escalate a dispute.")
        .WithTags("Finance | Disputes")
        .WithMetadata(new MustHavePermissionAttribute(FinanceFeatures.AdminFinanceDashboard, AppAction.Update))
        .RequireAuthorization();
    }
}

public sealed record OpenDisputeRequest(Guid PaymentId, string Reason, string Description);

public sealed record ResolveDisputeRequest(DisputeResolution Resolution, string? Notes);

public sealed record EscalateDisputeRequest(string Reason);
