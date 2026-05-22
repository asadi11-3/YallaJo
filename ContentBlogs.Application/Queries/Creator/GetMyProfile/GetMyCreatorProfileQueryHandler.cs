using ContentBlogs.Application.Queries.Creator.Dtos;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.Creator.GetMyProfile;

public sealed class GetMyCreatorProfileQueryHandler(
    ICreatorProfileRepository profileRepository,
    ICurrentUser currentUser,
    ILogger<GetMyCreatorProfileQueryHandler> logger)
    : IQueryHandler<GetMyCreatorProfileQuery, CreatorProfileDto?>
{
    public async Task<Result<CreatorProfileDto?>> Handle(
        GetMyCreatorProfileQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.UserId is null)
            {
                return Result.Failure<CreatorProfileDto?>(
                    new Error("Auth.UserIdMissing", "Authenticated user id is missing."),
                    Outcome.Unauthorized);
            }

            var profile = await profileRepository
                .GetByUserIdAsync(currentUser.UserId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (profile is null)
            {
                return Result<CreatorProfileDto?>.Success(null);
            }

            var dto = new CreatorProfileDto(
                profile.Id,
                profile.UserId,
                profile.Slug,
                profile.DisplayName,
                profile.Bio,
                profile.AvatarUrl,
                profile.TrustTier,
                profile.Status,
                profile.ArticleCount,
                profile.TotalViewCount,
                profile.TotalReactionCount,
                profile.TotalCommentCount,
                profile.FollowerCount,
                profile.LinkedProviderId,
                profile.CreatedAt);

            return Result<CreatorProfileDto?>.Success(dto);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<CreatorProfileDto?>(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
