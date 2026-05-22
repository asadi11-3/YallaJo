using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Repositories;
using ContentBlogs.Domain.Validators;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Creator.Posts.PublishPost;

public sealed class PublishCreatorPostCommandHandler(
    ICreatorPostRepository postRepository,
    ICreatorProfileRepository profileRepository,
    IProviderEntitiesReadClient providerEntitiesClient,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<PublishCreatorPostCommandHandler> logger)
    : ICommandHandler<PublishCreatorPostCommand>
{
    public async Task<Result> Handle(
        PublishCreatorPostCommand request,
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

            var profile = await profileRepository
                .GetByUserIdAsync(currentUser.UserId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (profile is null)
            {
                return Result.Failure(
                    new Error("Creator.ProfileNotFound", "You do not have a creator profile."),
                    Outcome.NotFound);
            }

            var post = await postRepository
                .GetByIdAsync(request.PostId, cancellationToken)
                .ConfigureAwait(false);

            if (post is null || post.CreatorProfileId != profile.Id)
            {
                return Result.Failure(
                    new Error("Post.NotFound", "Post not found."),
                    Outcome.NotFound);
            }

            // Type-specific data validation at publish time
            var typeValidation = CreatorPostTypeValidator.Validate(post.PostType, post.TypeSpecificDataJson);
            if (typeValidation.IsFailure)
            {
                return Result.Failure(
                    typeValidation.Errors.FirstOrDefault()!,
                    Outcome.UnprocessableEntity);
            }

            // Disclosure validation — defense-in-depth (spec §6.3)
            var ownedEntityIds = await providerEntitiesClient
                .GetOwnedEntityIdsAsync(currentUser.UserId.Value, cancellationToken)
                .ConfigureAwait(false);

            var disclosureResult = post.ValidateDisclosure(ownedEntityIds);
            if (disclosureResult.IsFailure)
            {
                return Result.Failure(
                    disclosureResult.Errors.FirstOrDefault()!,
                    Outcome.UnprocessableEntity);
            }

            var utcNow = DateTime.UtcNow;
            var publishResult = post.PublishDirect(profile.TrustTier, utcNow);

            if (publishResult.IsFailure)
            {
                return Result.Failure(
                    publishResult.Errors.FirstOrDefault()!,
                    Outcome.UnprocessableEntity);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.MyCreatorPostsTag(profile.Id), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorPostsListTag, cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorPostTag(post.Slug), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Creator post published directly: {PostId} by Tier-{Tier} creator {CreatorId}",
                post.Id, profile.TrustTier, profile.Id);

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
