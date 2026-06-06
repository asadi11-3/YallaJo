using ContentBlogs.Application.Queries.Creator.Dtos;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Queries.Creator.GetCreatorProfileBySlug;

public sealed class GetCreatorProfileBySlugQueryHandler(
    ICreatorProfileRepository profileRepository,
    ILogger<GetCreatorProfileBySlugQueryHandler> logger)
    : IQueryHandler<GetCreatorProfileBySlugQuery, PublicCreatorProfileDto>
{
    public async Task<Result<PublicCreatorProfileDto>> Handle(
        GetCreatorProfileBySlugQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var profile = await profileRepository
                .GetBySlugAsync(request.Slug, cancellationToken)
                .ConfigureAwait(false);

            if (profile is null || profile.Status != CreatorProfileStatus.Active)
            {
                return Result.Failure<PublicCreatorProfileDto>(
                    CreatorProfileErrors.NotFound, Outcome.NotFound);
            }

            // Public-safe projection (Gap 4): never expose UserId, internal Status,
            // LinkedProviderId, or CreatedAt on this anonymous endpoint.
            var dto = new PublicCreatorProfileDto(
                profile.Id,
                profile.Slug,
                profile.DisplayName,
                profile.Bio,
                profile.AvatarUrl,
                profile.TrustTier,
                profile.ArticleCount,
                profile.TotalViewCount,
                profile.TotalReactionCount,
                profile.TotalCommentCount,
                profile.FollowerCount);

            return Result<PublicCreatorProfileDto>.Success(dto);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<PublicCreatorProfileDto>(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
