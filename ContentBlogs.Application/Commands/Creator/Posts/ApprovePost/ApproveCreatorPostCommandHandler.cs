using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

// Disclosure validation (defense-in-depth, spec §6.3)

namespace ContentBlogs.Application.Commands.Creator.Posts.ApprovePost;

public sealed class ApproveCreatorPostCommandHandler(
    ICreatorPostRepository postRepository,
    ICreatorProfileRepository profileRepository,
    IProviderEntitiesReadClient providerEntitiesClient,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<ApproveCreatorPostCommandHandler> logger)
    : ICommandHandler<ApproveCreatorPostCommand>
{
    public async Task<Result> Handle(
        ApproveCreatorPostCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.UserId is null)
            {
                return Result.Failure(
                    new Error("Auth.UserIdMissing", "Authenticated user id is missing."),
                    Outcome.Unauthorized);
            }

            var post = await postRepository
                .GetByIdAsync(request.PostId, cancellationToken)
                .ConfigureAwait(false);

            if (post is null)
            {
                return Result.Failure(CreatorPostErrors.NotFound, Outcome.NotFound);
            }

            // Disclosure validation — defense-in-depth (spec §6.3)
            var creatorProfile = await profileRepository
                .GetByIdAsync(post.CreatorProfileId, cancellationToken)
                .ConfigureAwait(false);

            if (creatorProfile is null)
            {
                return Result.Failure(
                    new Error("Creator.ProfileNotFound", "Creator profile not found for this post."),
                    Outcome.NotFound);
            }

            var ownedEntityIds = await providerEntitiesClient
                .GetOwnedEntityIdsAsync(creatorProfile.UserId, cancellationToken)
                .ConfigureAwait(false);

            var disclosureResult = post.ValidateDisclosure(ownedEntityIds);
            if (disclosureResult.IsFailure)
            {
                return Result.Failure(
                    disclosureResult.Errors.FirstOrDefault()!,
                    Outcome.UnprocessableEntity);
            }

            var approveResult = post.ApproveByAdmin(currentUser.UserId.Value, DateTime.UtcNow);

            if (approveResult.IsFailure)
            {
                return Result.Failure(
                    approveResult.Errors.FirstOrDefault()!,
                    Outcome.UnprocessableEntity);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.AdminPostsQueueTag, cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorPostsListTag, cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.MyCreatorPostsTag(post.CreatorProfileId), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorPostTag(post.Slug), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Creator post approved: {PostId} by admin {AdminId}",
                post.Id, currentUser.UserId.Value);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
