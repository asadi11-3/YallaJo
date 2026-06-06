using ContentBlogs.Application.Commands.Creator.CreateApplication;
using ContentBlogs.Application.Commands.Creator.FollowCreator;
using ContentBlogs.Application.Commands.Creator.RedeemInvitation;
using ContentBlogs.Application.Commands.Creator.SelfDeactivate;
using ContentBlogs.Application.Commands.Creator.SubmitApplication;
using ContentBlogs.Application.Commands.Creator.UnfollowCreator;
using ContentBlogs.Application.Commands.Creator.UpdateApplication;
using ContentBlogs.Application.Commands.Creator.UpdateAvatar;
using ContentBlogs.Application.Commands.Creator.UpdateProfile;
using ContentBlogs.Application.Queries.Blog.GetCreatorBlogs;
using ContentBlogs.Application.Queries.Blog.GetCreatorBlogsBySlug;
using ContentBlogs.Application.Queries.Creator.GetCreatorProfileBySlug;
using ContentBlogs.Application.Queries.Creator.GetMyApplication;
using ContentBlogs.Application.Queries.Creator.GetMyProfile;
using ContentBlogs.Application.Queries.Creator.IsFollowingCreator;
using ContentBlogs.Application.Queries.Creator.ListCreatorFollowers;
using ContentBlogs.Application.Queries.Creator.ListCreatorNiches;
using ContentBlogs.Application.Queries.Blog.Dtos;
using ContentBlogs.Application.Queries.Creator.Dtos;
using ContentBlogs.Contracts.Authorization;
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

internal static class CreatorEndpoints
{
    internal static void MapCreatorEndpoints(RouteGroupBuilder blogGroup)
    {
        var group = blogGroup.MapGroup("/creators")
            .WithTags("ContentBlogs | Creators");

        // ── GET /api/v1/blogs/creators/niches ─────────────────────────────
        group.MapGet("/niches", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListCreatorNichesQuery(), ct);
            return result.ToApiResult();
        })
        .WithName("ListCreatorNiches")
        .WithSummary("List all active creator niches")
        .Produces<List<CreatorNicheDto>>(StatusCodes.Status200OK)
        .AllowAnonymous();

        // ── GET /api/v1/blogs/creators/profiles/{slug} ───────────────────
        group.MapGet("/profiles/{slug}", async (
            string slug,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCreatorProfileBySlugQuery(slug), ct);
            return result.ToApiResult();
        })
        .WithName("GetCreatorProfileBySlug")
        .WithSummary("Get a public creator profile by slug")
        .Produces<PublicCreatorProfileDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();

        // ── GET /api/v1/blogs/creators/profiles/{profileId}/followers ─────
        group.MapGet("/profiles/{profileId:guid}/followers", async (
            Guid profileId,
            int? page,
            int? pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new ListCreatorFollowersQuery(profileId, page ?? 1, pageSize ?? 20), ct);
            return result.ToApiResult();
        })
        .WithName("ListCreatorFollowers")
        .WithSummary("List followers of a creator (public-safe: no user IDs)")
        .Produces<IReadOnlyList<FollowerSummaryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();

        // ── POST /api/v1/blogs/creators/applications ─────────────────────
        group.MapPost("/applications", async (
            CreateCreatorApplicationRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new CreateCreatorApplicationCommand(
                Bio: request.Bio,
                PortfolioUrls: request.PortfolioUrls,
                SampleWorkUrls: request.SampleWorkUrls,
                NicheIds: request.NicheIds,
                FreeTags: request.FreeTags,
                LanguageIds: request.LanguageIds,
                PreferredRegionIds: request.PreferredRegionIds,
                SocialHandles: request.SocialHandles);

            var result = await sender.Send(cmd, ct);
            return result.ToApiResult(r => $"/api/v1/blogs/creators/applications/{r.ApplicationId}");
        })
        .WithName("CreateCreatorApplication")
        .WithSummary("Create a new creator application (Draft)")
        .Produces<CreateCreatorApplicationResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Creator, AppAction.Submit));

        // ── GET /api/v1/blogs/creators/applications/mine ─────────────────
        group.MapGet("/applications/mine", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyCreatorApplicationQuery(), ct);
            return result.ToApiResult();
        })
        .WithName("GetMyCreatorApplication")
        .WithSummary("Get current user's latest creator application")
        .Produces<CreatorApplicationDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Creator, AppAction.Read));

        // ── PUT /api/v1/blogs/creators/applications/{applicationId} ──────
        group.MapPut("/applications/{applicationId:guid}", async (
            Guid applicationId,
            UpdateCreatorApplicationRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new UpdateCreatorApplicationCommand(
                ApplicationId: applicationId,
                Bio: request.Bio,
                PortfolioUrls: request.PortfolioUrls,
                SampleWorkUrls: request.SampleWorkUrls,
                NicheIds: request.NicheIds,
                FreeTags: request.FreeTags,
                LanguageIds: request.LanguageIds,
                PreferredRegionIds: request.PreferredRegionIds,
                SocialHandles: request.SocialHandles);

            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("UpdateCreatorApplication")
        .WithSummary("Update a Draft/MoreInfoNeeded creator application")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Creator, AppAction.Update));

        // ── POST /api/v1/blogs/creators/applications/{applicationId}/submit
        group.MapPost("/applications/{applicationId:guid}/submit", async (
            Guid applicationId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new SubmitCreatorApplicationCommand(applicationId), ct);
            return result.ToApiResult();
        })
        .WithName("SubmitCreatorApplication")
        .WithSummary("Submit a Draft application for review")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Creator, AppAction.Submit));

        // ── GET /api/v1/blogs/creators/profile/mine ──────────────────────
        group.MapGet("/profile/mine", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyCreatorProfileQuery(), ct);
            return result.ToApiResult();
        })
        .WithName("GetMyCreatorProfile")
        .WithSummary("Get current user's creator profile")
        .Produces<CreatorProfileDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Creator, AppAction.Read));

        // ── PUT /api/v1/blogs/creators/profile/mine ──────────────────────
        group.MapPut("/profile/mine", async (
            UpdateCreatorProfileRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new UpdateCreatorProfileCommand(
                DisplayName: request.DisplayName,
                Bio: request.Bio,
                AvatarUrl: request.AvatarUrl,
                Slug: request.NewSlug);

            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("UpdateMyCreatorProfile")
        .WithSummary("Update current user's creator profile")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Creator, AppAction.Update));

        // ── POST /api/v1/blogs/creators/profiles/{profileId}/follow ──────
        group.MapPost("/profiles/{profileId:guid}/follow", async (
            Guid profileId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new FollowCreatorCommand(profileId), ct);
            return result.ToApiResult();
        })
        .WithName("FollowCreator")
        .WithSummary("Follow a creator")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Creator, AppAction.Follow));

        // ── DELETE /api/v1/blogs/creators/profiles/{profileId}/follow ─────
        group.MapDelete("/profiles/{profileId:guid}/follow", async (
            Guid profileId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new UnfollowCreatorCommand(profileId), ct);
            return result.ToApiResult();
        })
        .WithName("UnfollowCreator")
        .WithSummary("Unfollow a creator")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Creator, AppAction.Unfollow));

        // ── GET /api/v1/blogs/creators/profiles/{profileId}/following ─────
        group.MapGet("/profiles/{profileId:guid}/following", async (
            Guid profileId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new IsFollowingCreatorQuery(profileId), ct);
            return result.ToApiResult();
        })
        .WithName("IsFollowingCreator")
        .WithSummary("Check if current user is following a creator")
        .Produces<bool>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Creator, AppAction.Read));

        // ── POST /api/v1/blogs/creators/invitations/redeem ───────────────
        group.MapPost("/invitations/redeem", async (
            RedeemCreatorInvitationRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var cmd = new RedeemCreatorInvitationCommand(request.Token);
            var result = await sender.Send(cmd, ct);
            return result.ToApiResult();
        })
        .WithName("RedeemCreatorInvitation")
        .WithSummary("Redeem a creator invitation token")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Creator, AppAction.RedeemInvitation));

        // ── DELETE /api/v1/blogs/creators/profile/mine ───────────────────
        group.MapDelete("/profile/mine", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SelfDeactivateCreatorProfileCommand(), ct);
            return result.ToApiResult();
        })
        .WithName("SelfDeactivateCreatorProfile")
        .WithSummary("Creator voluntarily deactivates own profile (soft delete, 60-day hard delete)")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Creator, AppAction.Delete));

        // ── PUT /api/v1/blogs/creators/profile/mine/avatar ───────────────
        group.MapPut("/profile/mine/avatar", async (
            UpdateCreatorAvatarRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateCreatorAvatarCommand(request.AvatarUrl), ct);
            return result.ToApiResult();
        })
        .WithName("UpdateCreatorAvatar")
        .WithSummary("Update creator avatar URL")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithMetadata(new MustHavePermissionAttribute(ContentBlogsFeatures.Creator, AppAction.Update));

        // ── GET /api/v1/blogs/creators/profiles/{slug}/blogs ─────────────
        group.MapGet("/profiles/{slug}/blogs", async (
            string slug,
            int? page,
            int? pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            // Caller provides a CreatorProfile slug — we need the profile ID.
            // GetCreatorBlogsQuery accepts creatorProfileId.
            // For now we use slug-based query; caller can also use profile id directly.
            // Phase 4 will provide a GetCreatorProfileBySlugQuery result to get the id first.
            // Using a separate endpoint that forwards to GetCreatorBlogsQuery via slug lookup
            // would require an extra repo call. Instead we delegate to query which takes slug.
            var result = await sender.Send(
                new GetCreatorBlogsBySlugQuery(slug, page ?? 1, pageSize ?? 20), ct);
            return result.ToApiResult();
        })
        .WithName("GetCreatorBlogsBySlug")
        .WithSummary("List published blogs by creator slug (public)")
        .Produces<PaginatedResult<BlogSummaryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous();
    }
}
