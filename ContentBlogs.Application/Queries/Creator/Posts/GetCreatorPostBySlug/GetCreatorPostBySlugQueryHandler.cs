using ContentBlogs.Application.Queries.Creator.Dtos;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.Creator.Posts.GetCreatorPostBySlug;

public sealed class GetCreatorPostBySlugQueryHandler(
    ICreatorPostRepository postRepository,
    ICreatorProfileRepository profileRepository,
    ILogger<GetCreatorPostBySlugQueryHandler> logger)
    : IQueryHandler<GetCreatorPostBySlugQuery, CreatorPostDto>
{
    public async Task<Result<CreatorPostDto>> Handle(
        GetCreatorPostBySlugQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var post = await postRepository.GetBySlugAsync(request.Slug, cancellationToken)
                .ConfigureAwait(false);

            if (post is null)
                return Result<CreatorPostDto>.Failure(
                    CreatorPostErrors.NotFound, Outcome.NotFound);

            if (post.Status != CreatorPostStatus.Published)
                return Result<CreatorPostDto>.Failure(
                    CreatorPostErrors.NotFound, Outcome.NotFound);

            var profile = await profileRepository.GetByIdAsync(post.CreatorProfileId, cancellationToken)
                .ConfigureAwait(false);

            var dto = new CreatorPostDto(
                post.Id,
                post.CreatorProfileId,
                profile?.DisplayName ?? "Unknown",
                profile?.Slug ?? string.Empty,
                post.PostType,
                post.Slug,
                post.Title,
                post.Excerpt,
                post.Body,
                post.LanguageId,
                post.Status,
                post.SubmittedAt,
                post.ReviewedAt,
                post.ReviewedByAdminId,
                post.PublishedAt,
                post.RejectionReason,
                post.IsFeatured,
                post.FeaturedAt,
                post.FeaturedByAdminId,
                post.FeaturedUntil,
                post.IsSponsored,
                post.DisclosedTargets,
                post.ViewCount,
                post.ReactionCount,
                post.CommentCount,
                post.ReportCount,
                post.NicheIds,
                post.FreeTags,
                post.TypeSpecificDataJson,
                post.CreatedAt);

            logger.LogDebug("GetCreatorPostBySlug: slug={Slug} found={PostId}", request.Slug, post.Id);

            return Result<CreatorPostDto>.Success(dto);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<CreatorPostDto>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
