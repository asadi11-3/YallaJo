using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.Creator.IsFollowingCreator;

public sealed class IsFollowingCreatorQueryHandler(
    ICreatorFollowRepository followRepository,
    ICurrentUser currentUser,
    ILogger<IsFollowingCreatorQueryHandler> logger)
    : IQueryHandler<IsFollowingCreatorQuery, bool>
{
    public async Task<Result<bool>> Handle(
        IsFollowingCreatorQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var isFollowing = await followRepository
                .ExistsAsync(currentUser.UserId!.Value, request.CreatorProfileId, cancellationToken)
                .ConfigureAwait(false);

            return Result<bool>.Success(isFollowing);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<bool>(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
