using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.Creator.ListCreatorFollowers;

public sealed class ListCreatorFollowersQueryHandler(
    ICreatorFollowRepository followRepository,
    ILogger<ListCreatorFollowersQueryHandler> logger)
    : IQueryHandler<ListCreatorFollowersQuery, IReadOnlyList<Guid>>
{
    public async Task<Result<IReadOnlyList<Guid>>> Handle(
        ListCreatorFollowersQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var followerIds = await followRepository
                .GetFollowerUserIdsAsync(request.CreatorProfileId, request.Page, request.PageSize, cancellationToken)
                .ConfigureAwait(false);

            return Result<IReadOnlyList<Guid>>.Success(followerIds);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<IReadOnlyList<Guid>>(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
