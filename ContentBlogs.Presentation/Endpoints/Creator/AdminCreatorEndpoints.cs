using ContentBlogs.Application.Commands.Creator.ApproveApplication;
using ContentBlogs.Application.Commands.Creator.ReinstateProfile;
using ContentBlogs.Application.Commands.Creator.RejectApplication;
using ContentBlogs.Application.Commands.Creator.RequestMoreInfo;
using ContentBlogs.Application.Commands.Creator.SendInvitation;
using ContentBlogs.Application.Commands.Creator.SuspendProfile;
using ContentBlogs.Application.Queries.Creator.AdminGetApplication;
using ContentBlogs.Application.Queries.Creator.AdminListApplications;
using ContentBlogs.Application.Queries.Creator.Dtos;
using ContentBlogs.Contracts.Authorization;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Presentation.Endpoints.Creator.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace ContentBlogs.Presentation.Endpoints.Creator;

internal static class AdminCreatorEndpoints
{
    internal static void MapAdminCreatorEndpoints(RouteGroupBuilder blogGroup)
    {
        var group = blogGroup.MapGroup("/admin/creators")
            .WithTags("Admin - Creators");

        // ── GET /api/v1/blogs/admin/creators/applications ────────────────
        group.MapGet("/applications", async (
            int? page,
            int? pageSize,
            CreatorApplicationStatus? status,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new AdminListCreatorApplicationsQuery(page ?? 1, pageSize ?? 20, status), ct);
            return result.ToApiResult();
        })
        .WithName("AdminListCreatorApplications")
        .WithSummary("Admin — list creator applications with optional status filter")
        .Produces<PaginatedResult<CreatorApplicationSummaryDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.AdminCreatorQueue, AppAction.Read));

        // ── GET /api/v1/blogs/admin/creators/applications/{id} ───────────
        group.MapGet("/applications/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new AdminGetCreatorApplicationQuery(id), ct);
            return result.ToApiResult();
        })
        .WithName("AdminGetCreatorApplication")
        .WithSummary("Admin — get a single creator application by ID")
        .Produces<CreatorApplicationDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.AdminCreatorQueue, AppAction.Read));

        // ── POST /api/v1/blogs/admin/creators/applications/{id}/approve ──
        group.MapPost("/applications/{id:guid}/approve", async (
            Guid id,
            ApproveApplicationRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new ApproveCreatorApplicationCommand(id, request.DisplayName, request.AvatarUrl);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("AdminApproveCreatorApplication")
        .WithSummary("Admin — approve a pending creator application")
        .Produces<ApproveCreatorApplicationResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.AdminCreatorQueue, AppAction.Approve));

        // ── POST /api/v1/blogs/admin/creators/applications/{id}/reject ───
        group.MapPost("/applications/{id:guid}/reject", async (
            Guid id,
            RejectApplicationRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new RejectCreatorApplicationCommand(id, request.Reason);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("AdminRejectCreatorApplication")
        .WithSummary("Admin — reject a pending creator application")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.AdminCreatorQueue, AppAction.Reject));

        // ── POST /api/v1/blogs/admin/creators/applications/{id}/request-more-info
        group.MapPost("/applications/{id:guid}/request-more-info", async (
            Guid id,
            RequestMoreInfoRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new RequestMoreInfoCommand(id, request.AdminNote);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("AdminRequestMoreInfoCreatorApplication")
        .WithSummary("Admin — request additional information on a pending application")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.AdminCreatorQueue, AppAction.RequestMoreInfo));

        // ── POST /api/v1/blogs/admin/creators/profiles/{profileId}/suspend
        group.MapPost("/profiles/{profileId:guid}/suspend", async (
            Guid profileId,
            SuspendProfileRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new SuspendCreatorProfileCommand(profileId, request.Reason);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("AdminSuspendCreatorProfile")
        .WithSummary("Admin — suspend a creator profile")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.AdminCreatorQueue, AppAction.Suspend));

        // ── POST /api/v1/blogs/admin/creators/profiles/{profileId}/reinstate
        group.MapPost("/profiles/{profileId:guid}/reinstate", async (
            Guid profileId,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new ReinstateCreatorProfileCommand(profileId);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("AdminReinstateCreatorProfile")
        .WithSummary("Admin — reinstate a suspended creator profile")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.AdminCreatorQueue, AppAction.Reinstate));

        // ── POST /api/v1/blogs/admin/creators/profiles/{profileId}/promote
        group.MapPost("/profiles/{profileId:guid}/promote", async (
            Guid profileId,
            Models.PromoteCreatorTierRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new Application.Commands.Creator.Posts.PromoteTier.PromoteCreatorTierCommand(
                profileId, request.TargetTier);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("AdminPromoteCreatorTier")
        .WithSummary("Admin — promote a creator to a higher trust tier")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.AdminCreatorQueue, AppAction.PromoteTier));

        // ── POST /api/v1/blogs/admin/creators/profiles/{profileId}/demote
        group.MapPost("/profiles/{profileId:guid}/demote", async (
            Guid profileId,
            Models.DemoteCreatorTierRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new Application.Commands.Creator.Posts.DemoteTier.DemoteCreatorTierCommand(
                profileId, request.TargetTier, request.Reason);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("AdminDemoteCreatorTier")
        .WithSummary("Admin — demote a creator to a lower trust tier")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.AdminCreatorQueue, AppAction.DemoteTier));

        // ── POST /api/v1/blogs/admin/creators/invitations ────────────────
        group.MapPost("/invitations", async (
            SendInvitationRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new SendCreatorInvitationCommand(
                Kind: request.Kind,
                Email: request.Email,
                InvitedUserId: request.InvitedUserId,
                PersonalMessage: request.PersonalMessage);

            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("AdminSendCreatorInvitation")
        .WithSummary("Admin — send a creator invitation via email or in-app")
        .Produces<SendCreatorInvitationResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.AdminCreatorQueue, AppAction.Invite));
    }
}
