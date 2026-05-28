using ContentBlogs.Application.Queries.Creator.Dtos;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.Creator.AdminGetProfile;

public sealed class AdminGetCreatorProfileQueryHandler(
    ICreatorProfileRepository profileRepository,
    ILogger<AdminGetCreatorProfileQueryHandler> logger)
    : IQueryHandler<AdminGetCreatorProfileQuery, CreatorProfileDto?>
{
    public async Task<Result<CreatorProfileDto?>> Handle(
        AdminGetCreatorProfileQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var profile = await profileRepository
                .GetByIdAsync(request.ProfileId, cancellationToken)
                .ConfigureAwait(false);

            if (profile is null)
                return Result<CreatorProfileDto?>.Failure(CreatorProfileErrors.NotFound, Outcome.NotFound);

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
