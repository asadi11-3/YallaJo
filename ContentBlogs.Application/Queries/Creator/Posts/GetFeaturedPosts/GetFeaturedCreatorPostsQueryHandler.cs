using ContentBlogs.Application.Queries.Creator.Dtos;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.Creator.Posts.GetFeaturedPosts;

public sealed class GetFeaturedCreatorPostsQueryHandler(
    ICreatorPostRepository postRepository,
    ICreatorProfileRepository profileRepository,
    ILogger<GetFeaturedCreatorPostsQueryHandler> logger)
    : IQueryHandler<GetFeaturedCreatorPostsQuery, IReadOnlyList<CreatorPostSummaryDto>>
{
    public async Task<Result<IReadOnlyList<CreatorPostSummaryDto>>> Handle(
        GetFeaturedCreatorPostsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var limit = Math.Clamp(request.Limit, 1, 50);

            var posts = await postRepository
                .SelectAsync(
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
                    filter: post => post.Status == CreatorPostStatus.Published
                        && post.IsFeatured
                        && (post.FeaturedUntil == null || post.FeaturedUntil > DateTime.UtcNow),
                    orderBy: q => q.OrderByDescending(p => p.FeaturedAt),
                    ct: cancellationToken)
                .ConfigureAwait(false);

            var limited = posts.Take(limit).ToList();

            // Hydrate creator names
            var profileIds = limited.Select(p => p.CreatorProfileId).Distinct().ToList();
            var profiles = new Dictionary<Guid, (string DisplayName, string Slug)>();
            foreach (var profileId in profileIds)
            {
                var profile = await profileRepository.GetByIdAsync(profileId, cancellationToken)
                    .ConfigureAwait(false);
                if (profile is not null)
                    profiles[profileId] = (profile.DisplayName, profile.Slug);
            }

            var result = limited.Select(item =>
            {
                profiles.TryGetValue(item.CreatorProfileId, out var info);
                return item with
                {
                    CreatorDisplayName = info.DisplayName ?? "Unknown",
                    CreatorSlug = info.Slug ?? string.Empty,
                };
            }).ToList();

            logger.LogDebug("GetFeaturedCreatorPosts: count={Count}", result.Count);

            return Result<IReadOnlyList<CreatorPostSummaryDto>>.Success(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<IReadOnlyList<CreatorPostSummaryDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
