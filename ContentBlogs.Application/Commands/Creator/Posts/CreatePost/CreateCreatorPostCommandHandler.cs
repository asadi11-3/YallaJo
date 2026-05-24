using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Commands.Creator.Common;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Entities.Creators;
using ContentBlogs.Domain.Repositories;
using ContentBlogs.Domain.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Creator.Posts.CreatePost;

public sealed class CreateCreatorPostCommandHandler(
    ICreatorPostRepository postRepository,
    ICreatorProfileRepository profileRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<CreateCreatorPostCommandHandler> logger)
    : ICommandHandler<CreateCreatorPostCommand, CreateCreatorPostResult>
{
    public async Task<Result<CreateCreatorPostResult>> Handle(
        CreateCreatorPostCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
                        var profile = await profileRepository
                .GetByUserIdAsync(currentUser.UserId!.Value, cancellationToken)
                .ConfigureAwait(false);

            if (profile is null)
            {
                return Result.Failure<CreateCreatorPostResult>(
                    new Error("Creator.ProfileNotFound", "You do not have a creator profile."),
                    Outcome.NotFound);
            }

            // Validate type-specific data
            var typeValidation = CreatorPostTypeValidator.Validate(request.PostType, request.TypeSpecificDataJson);
            if (typeValidation.IsFailure)
            {
                return Result.Failure<CreateCreatorPostResult>(
                    typeValidation.Errors.FirstOrDefault()!,
                    Outcome.UnprocessableEntity);
            }

            // Generate unique slug
            var existingSlugs = await postRepository
                .GetAllSlugsAsync(cancellationToken)
                .ConfigureAwait(false);

            var slug = CreatorSlugGenerator.Generate(request.Title, existingSlugs);

            var createResult = CreatorPost.Create(
                creatorProfileId: profile.Id,
                postType: request.PostType,
                slug: slug,
                title: request.Title,
                excerpt: request.Excerpt,
                languageId: request.LanguageId,
                typeSpecificDataJson: request.TypeSpecificDataJson);

            if (createResult.IsFailure)
            {
                return Result.Failure<CreateCreatorPostResult>(
                    createResult.Errors.FirstOrDefault()!,
                    Outcome.UnprocessableEntity);
            }

            var post = createResult.Value;

            // Apply tags if provided
            if (request.NicheIds?.Count > 0 || request.FreeTags?.Count > 0)
            {
                var tagResult = post.Tag(
                    [],      // taggedEntityIds – set later via separate Tag command
                    [],      // taggedEntityTypes
                    request.NicheIds ?? [],
                    request.FreeTags ?? [],
                    []);     // placeRegionIds

                if (tagResult.IsFailure)
                {
                    return Result.Failure<CreateCreatorPostResult>(
                        tagResult.Errors.FirstOrDefault()!,
                        Outcome.UnprocessableEntity);
                }
            }

            await postRepository.AddAsync(post, cancellationToken).ConfigureAwait(false);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure<CreateCreatorPostResult>(
                    new Error("Post.ConcurrencyConflict",
                        "Post was modified by another request. Please retry."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.MyCreatorPostsTag(profile.Id), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Creator post created: {PostId} (Slug={Slug}, Type={Type}, CreatorId={CreatorId})",
                post.Id, post.Slug, post.PostType, profile.Id);

            return Result.Created(new CreateCreatorPostResult(post.Id, post.Slug));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<CreateCreatorPostResult>(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
