using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Creator.AdminUpdate;

public sealed class AdminUpdateCreatorProfileCommandHandler(
    ICreatorProfileRepository profileRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<AdminUpdateCreatorProfileCommandHandler> logger)
    : ICommandHandler<AdminUpdateCreatorProfileCommand>
{
    public async Task<Result> Handle(
        AdminUpdateCreatorProfileCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var profile = await profileRepository
                .GetByIdAsync(request.ProfileId, cancellationToken, asNoTracking: false)
                .ConfigureAwait(false);

            if (profile is null)
                return Result.Failure(CreatorProfileErrors.NotFound, Outcome.NotFound);

            var updateResult = profile.UpdateProfile(request.DisplayName, request.Bio, request.AvatarUrl);
            if (updateResult.IsFailure)
                return updateResult;

            if (request.CoverImageUrl is not null)
                profile.UpdateCoverImage(request.CoverImageUrl);

            if (!string.IsNullOrEmpty(request.Slug) && request.Slug != profile.Slug)
            {
                if (await profileRepository.IsSlugTakenAsync(request.Slug, profile.Id, cancellationToken).ConfigureAwait(false))
                    return Result.Failure(CreatorProfileErrors.SlugAlreadyTaken, Outcome.Conflict);

                var slugResult = profile.ChangeSlug(request.Slug);
                if (slugResult.IsFailure)
                    return slugResult;
            }

            profileRepository.Update(profile);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error("Creator.ConcurrencyConflict", "Profile was modified concurrently."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentBlogsCacheKeys.CreatorProfileTag(profile.Id), cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync(ContentBlogsCacheKeys.CreatorProfileUserTag(profile.UserId), cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Admin updated CreatorProfile {ProfileId}", profile.Id);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }
}
