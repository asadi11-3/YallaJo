using ContentBlogs.Application.Queries.Creator.Dtos;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.Creator.ListCreatorFollowers;

public sealed class ListCreatorFollowersQueryHandler(
    ICreatorFollowRepository followRepository,
    ILogger<ListCreatorFollowersQueryHandler> logger)
    : IQueryHandler<ListCreatorFollowersQuery, IReadOnlyList<FollowerSummaryDto>>
{
    public async Task<Result<IReadOnlyList<FollowerSummaryDto>>> Handle(
        ListCreatorFollowersQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Max(1, request.PageSize);

            // Public-safe (Gap 3 Phase A): only followed-at timestamps are loaded —
            // never follower user IDs. The ordinal is a page-aware display index.
            var timestamps = await followRepository
                .GetFollowerTimestampsAsync(request.CreatorProfileId, page, pageSize, cancellationToken)
                .ConfigureAwait(false);

            var startOrdinal = ((page - 1) * pageSize) + 1;
            var followers = timestamps
                .Select((followedAt, index) => new FollowerSummaryDto(startOrdinal + index, followedAt))
                .ToList();

            return Result<IReadOnlyList<FollowerSummaryDto>>.Success(followers);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<IReadOnlyList<FollowerSummaryDto>>(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
