using ContentBlogs.Application.Queries.Creator.Dtos;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.Creator.Posts.GetAdminPostQueue;

public sealed class GetAdminPostQueueQueryHandler(
    ICreatorPostRepository postRepository,
    ICreatorProfileRepository profileRepository,
    ILogger<GetAdminPostQueueQueryHandler> logger)
    : IQueryHandler<GetAdminPostQueueQuery, PaginatedResult<CreatorPostSummaryDto>>
{
    private const int MaxPageSize = 100;

    public async Task<Result<PaginatedResult<CreatorPostSummaryDto>>> Handle(
        GetAdminPostQueueQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

            // Default to PendingReview when no filter specified (admin queue primary use case)
            var statusFilter = request.StatusFilter ?? CreatorPostStatus.PendingReview;

            var paged = await postRepository
                .SelectPaginatedAsync(
                    pageNumber: page,
                    pageSize: pageSize,
                    selector: post => new CreatorPostSummaryDto(
                        post.Id,
                        post.CreatorProfileId,
                        string.Empty,
                        string.Empty,
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
                    filter: post => post.Status == statusFilter
                        && (request.TypeFilter == null || post.PostType == request.TypeFilter),
                    orderBy: q => q.OrderBy(p => p.SubmittedAt ?? p.CreatedAt),
                    ct: cancellationToken)
                .ConfigureAwait(false);

            // Hydrate creator names
            var profileIds = paged.Items.Select(p => p.CreatorProfileId).Distinct().ToList();
            var profiles = new Dictionary<Guid, (string DisplayName, string Slug)>();
            foreach (var profileId in profileIds)
            {
                var profile = await profileRepository.GetByIdAsync(profileId, cancellationToken)
                    .ConfigureAwait(false);
                if (profile is not null)
                    profiles[profileId] = (profile.DisplayName, profile.Slug);
            }

            var hydrated = paged.Items.Select(item =>
            {
                profiles.TryGetValue(item.CreatorProfileId, out var info);
                return item with
                {
                    CreatorDisplayName = info.DisplayName ?? "Unknown",
                    CreatorSlug = info.Slug ?? string.Empty,
                };
            }).ToList();

            var result = new PaginatedResult<CreatorPostSummaryDto>(
                hydrated, paged.TotalCount, paged.PageNumber, paged.PageSize);

            logger.LogDebug(
                "GetAdminPostQueue: status={Status} page={Page} total={Total}",
                statusFilter, page, paged.TotalCount);

            return Result<PaginatedResult<CreatorPostSummaryDto>>.Success(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<PaginatedResult<CreatorPostSummaryDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
