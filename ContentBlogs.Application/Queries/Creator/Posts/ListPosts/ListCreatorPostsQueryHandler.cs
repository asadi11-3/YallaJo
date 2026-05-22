using ContentBlogs.Application.Queries.Creator.Dtos;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.Creator.Posts.ListPosts;

public sealed class ListCreatorPostsQueryHandler(
    ICreatorPostRepository postRepository,
    ICreatorProfileRepository profileRepository,
    ILogger<ListCreatorPostsQueryHandler> logger)
    : IQueryHandler<ListCreatorPostsQuery, PaginatedResult<CreatorPostSummaryDto>>
{
    private const int MaxPageSize = 100;

    public async Task<Result<PaginatedResult<CreatorPostSummaryDto>>> Handle(
        ListCreatorPostsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

            var search = string.IsNullOrWhiteSpace(request.Search)
                ? null
                : request.Search.Trim().ToLowerInvariant();

            var paged = await postRepository
                .SelectPaginatedAsync(
                    pageNumber: page,
                    pageSize: pageSize,
                    selector: post => new CreatorPostSummaryDto(
                        post.Id,
                        post.CreatorProfileId,
                        string.Empty, // populated below
                        string.Empty, // populated below
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
                    filter: post => post.Status == CreatorPostStatus.Published
                        && (request.CreatorProfileId == null || post.CreatorProfileId == request.CreatorProfileId)
                        && (request.TypeFilter == null || post.PostType == request.TypeFilter)
                        && (request.NicheId == null || post.NicheIds.Contains(request.NicheId.Value))
                        && (search == null
                            || post.Title.Contains(search)
                            || post.Slug.Contains(search)
                            || post.Excerpt.Contains(search)),
                    orderBy: q => q.OrderByDescending(p => p.PublishedAt)
                                   .ThenByDescending(p => p.CreatedAt),
                    ct: cancellationToken)
                .ConfigureAwait(false);

            // Hydrate creator names for the page of results
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
                "ListCreatorPosts: page={Page} total={Total} filters=(creator:{CreatorId},type:{Type})",
                page, paged.TotalCount, request.CreatorProfileId, request.TypeFilter);

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
