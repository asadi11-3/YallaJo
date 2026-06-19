using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Creator.UpdateProfile;

public sealed class UpdateCreatorProfileCommandHandler(
    ICreatorProfileRepository profileRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<UpdateCreatorProfileCommandHandler> logger)
    : ICommandHandler<UpdateCreatorProfileCommand>
{
    public async Task<Result> Handle(
        UpdateCreatorProfileCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
                        var profile = await profileRepository
                .GetByUserIdAsync(currentUser.UserId!.Value, cancellationToken)
                .ConfigureAwait(false);

            if (profile is null)
            {
                return Result.Failure(
                    CreatorProfileErrors.NotFound, Outcome.NotFound);
            }

            // ── Update basic fields ─────────────────────────────────────────
            // AvatarUrl is intentionally NOT accepted from profile-update input.
            // The managed avatar upload/clear endpoints are the only self-service
            // way to change a creator avatar, so the existing value is preserved.
            if (request.DisplayName is not null || request.Bio is not null)
            {
                var updateResult = profile.UpdateProfile(
                    request.DisplayName ?? profile.DisplayName,
                    request.Bio,
                    profile.AvatarUrl);

                if (updateResult.IsFailure)
                    return Result.Failure(updateResult.Errors.FirstOrDefault()!, Outcome.UnprocessableEntity);
            }

            // ── Change slug if requested ────────────────────────────────────
            if (!string.IsNullOrEmpty(request.Slug) && request.Slug != profile.Slug)
            {
                if (await profileRepository
                    .IsSlugTakenAsync(request.Slug, profile.Id, cancellationToken)
                    .ConfigureAwait(false))
                {
                    return Result.Failure(
                        CreatorProfileErrors.SlugAlreadyTaken, Outcome.Conflict);
                }

                var slugResult = profile.ChangeSlug(request.Slug);
                if (slugResult.IsFailure)
                    return Result.Failure(slugResult.Errors.FirstOrDefault()!, Outcome.UnprocessableEntity);
            }

            profileRepository.Update(profile);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error("Creator.ConcurrencyConflict",
                        "Profile was modified concurrently."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorProfileTag(profile.Id), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorProfileUserTag(profile.UserId), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "CreatorProfile updated: {ProfileId} (UserId={UserId})",
                profile.Id, profile.UserId);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
