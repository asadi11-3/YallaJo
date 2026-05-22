using ContentBlogs.Application.Queries.Creator.Dtos;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.Creator.Posts.GetMyCreatorPosts;

public sealed class GetMyCreatorPostsQueryHandler(
    ICreatorPostRepository postRepository,
    ICreatorProfileRepository profileRepository,
    ICurrentUser currentUser,
    ILogger<GetMyCreatorPostsQueryHandler> logger)
    : IQueryHandler<GetMyCreatorPostsQuery, PaginatedResult<CreatorPostSummaryDto>>
{
    private const int MaxPageSize = 100;

    public async Task<Result<PaginatedResult<CreatorPostSummaryDto>>> Handle(
        GetMyCreatorPostsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

            if (currentUser.UserId is null)
                return Result<PaginatedResult<CreatorPostSummaryDto>>.Failure(
                    new Error("Auth.UserIdMissing", "Authenticated user id is missing."),
                    Outcome.Unauthorized);

            var profile = await profileRepository.GetByUserIdAsync(currentUser.UserId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (profile is null)
                return Result<PaginatedResult<CreatorPostSummaryDto>>.Failure(
                    Domain.Errors.CreatorProfileErrors.NotFound, Outcome.NotFound);

            var paged = await postRepository
                .SelectPaginatedAsync(
                    pageNumber: page,
                    pageSize: pageSize,
                    selector: post => new CreatorPostSummaryDto(
                        post.Id,
                        post.CreatorProfileId,
                        profile.DisplayName,
                        profile.Slug,
                        post.PostType,
                        post.Slug,
                        post.Title,
                        post.Excerpt,
                        post.Status,
                        post.PublishedAt,
                        post.IsFeatured,
                        post.IsSponsored,
                        post.ViewCount,
                        post.ReactionCount,
                        post.CommentCount,
                        post.CreatedAt),
                    filter: post => post.CreatorProfileId == profile.Id
                        && (request.StatusFilter == null || post.Status == request.StatusFilter)
                        && (request.TypeFilter == null || post.PostType == request.TypeFilter),
                    orderBy: q => q.OrderByDescending(p => p.CreatedAt),
                    ct: cancellationToken)
                .ConfigureAwait(false);

            logger.LogDebug(
                "GetMyCreatorPosts: profileId={ProfileId} page={Page} total={Total}",
                profile.Id, page, paged.TotalCount);

            return Result<PaginatedResult<CreatorPostSummaryDto>>.Success(paged);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<PaginatedResult<CreatorPostSummaryDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
