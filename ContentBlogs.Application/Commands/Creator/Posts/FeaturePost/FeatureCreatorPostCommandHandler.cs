using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Entities.Creators;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Creator.Posts.FeaturePost;

public sealed class FeatureCreatorPostCommandHandler(
    ICreatorPostRepository postRepository,
    ICreatorProfileRepository profileRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<FeatureCreatorPostCommandHandler> logger)
    : ICommandHandler<FeatureCreatorPostCommand>
{
    public async Task<Result> Handle(
        FeatureCreatorPostCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
                        var post = await postRepository
                .GetByIdAsync(request.PostId, cancellationToken)
                .ConfigureAwait(false);

            if (post is null)
            {
                return Result.Failure(CreatorPostErrors.NotFound, Outcome.NotFound);
            }

            // Verify creator is Tier 2 (Expert)
            var profile = await profileRepository
                .GetByIdAsync(post.CreatorProfileId, cancellationToken)
                .ConfigureAwait(false);

            if (profile is null)
            {
                return Result.Failure(
                    new Error("Creator.ProfileNotFound", "Creator profile not found."),
                    Outcome.NotFound);
            }

            if (profile.TrustTier < Domain.Enums.CreatorTrustTier.Expert)
            {
                return Result.Failure(
                    CreatorPostErrors.TierInsufficientForFeaturing,
                    Outcome.UnprocessableEntity);
            }

            // Check global featured cap
            var currentFeaturedCount = await postRepository
                .CountFeaturedAsync(cancellationToken)
                .ConfigureAwait(false);

            if (currentFeaturedCount >= CreatorPost.MaxFeaturedGlobal)
            {
                logger.LogWarning(
                    "Feature cap reached ({Count}/{Max}). Proceeding as soft cap.",
                    currentFeaturedCount, CreatorPost.MaxFeaturedGlobal);
            }

            var featureResult = post.Feature(
                currentUser.UserId!.Value,
                request.FeaturedUntil,
                DateTime.UtcNow,
                profile.TrustTier);

            if (featureResult.IsFailure)
            {
                return Result.Failure(
                    featureResult.Errors.FirstOrDefault()!,
                    Outcome.UnprocessableEntity);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorPostsFeaturedTag, cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorPostTag(post.Slug), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorPostsListTag, cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Creator post featured: {PostId} by admin {AdminId}, until={Until}",
                post.Id, currentUser.UserId!.Value, request.FeaturedUntil);

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
