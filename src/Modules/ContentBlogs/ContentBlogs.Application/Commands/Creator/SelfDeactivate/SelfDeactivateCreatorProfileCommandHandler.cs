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

namespace ContentBlogs.Application.Commands.Creator.SelfDeactivate;

public sealed class SelfDeactivateCreatorProfileCommandHandler(
    ICreatorProfileRepository profileRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<SelfDeactivateCreatorProfileCommandHandler> logger)
    : ICommandHandler<SelfDeactivateCreatorProfileCommand>
{
    public async Task<Result> Handle(
        SelfDeactivateCreatorProfileCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var profile = await profileRepository
                .GetByUserIdAsync(currentUser.UserId!.Value, cancellationToken)
                .ConfigureAwait(false);

            if (profile is null)
                return Result.Failure(CreatorProfileErrors.NotFound, Outcome.NotFound);

            var result = profile.Deactivate(DateTime.UtcNow);
            if (result.IsFailure)
                return result;

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

            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorProfileTag(profile.Id), cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorProfileUserTag(profile.UserId), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Creator self-deactivated profile {ProfileId} (UserId={UserId})",
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
