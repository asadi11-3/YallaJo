using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Repositories;
using ContentBlogs.Domain.Validators;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Creator.Posts.UpdatePost;

public sealed class UpdateCreatorPostCommandHandler(
    ICreatorPostRepository postRepository,
    ICreatorProfileRepository profileRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<UpdateCreatorPostCommandHandler> logger)
    : ICommandHandler<UpdateCreatorPostCommand>
{
    public async Task<Result> Handle(
        UpdateCreatorPostCommand request,
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

            // Validate type-specific data if changed
            if (!string.IsNullOrWhiteSpace(request.TypeSpecificDataJson))
            {
                var typeValidation = CreatorPostTypeValidator.Validate(post.PostType, request.TypeSpecificDataJson);
                if (typeValidation.IsFailure)
                {
                    return Result.Failure(
                        typeValidation.Errors.FirstOrDefault()!,
                        Outcome.UnprocessableEntity);
                }
            }

            var updateResult = post.UpdateContent(
                request.Title,
                request.Excerpt,
                request.Body,
                request.TypeSpecificDataJson);

            if (updateResult.IsFailure)
            {
                return Result.Failure(
                    updateResult.Errors.FirstOrDefault()!,
                    Outcome.UnprocessableEntity);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.MyCreatorPostsTag(profile.Id), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorPostTag(post.Slug), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Creator post updated: {PostId} by creator {CreatorId}",
                post.Id, profile.Id);

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
