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

namespace ContentBlogs.Application.Commands.Creator.SuspendProfile;

public sealed class SuspendCreatorProfileCommandHandler(
    ICreatorProfileRepository profileRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<SuspendCreatorProfileCommandHandler> logger)
    : ICommandHandler<SuspendCreatorProfileCommand>
{
    public async Task<Result> Handle(
        SuspendCreatorProfileCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.UserId is null)
            {
                return Result.Failure(
                    new Error("Auth.UserIdMissing", "Authenticated user id is missing."),
                    Outcome.Unauthorized);
            }

            var profile = await profileRepository
                .GetByIdAsync(request.ProfileId, cancellationToken)
                .ConfigureAwait(false);

            if (profile is null)
            {
                return Result.Failure(
                    CreatorProfileErrors.NotFound, Outcome.NotFound);
            }

            var suspendResult = profile.Suspend(currentUser.UserId.Value, request.Reason);
            if (suspendResult.IsFailure)
            {
                return Result.Failure(suspendResult.Errors.FirstOrDefault()!, Outcome.UnprocessableEntity);
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
                "CreatorProfile suspended: {ProfileId} (AdminId={AdminId})",
                profile.Id, currentUser.UserId.Value);

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
