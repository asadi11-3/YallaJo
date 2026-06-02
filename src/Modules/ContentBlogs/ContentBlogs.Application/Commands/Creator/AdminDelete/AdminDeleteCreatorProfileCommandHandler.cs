using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Creator.AdminDelete;

public sealed class AdminDeleteCreatorProfileCommandHandler(
    ICreatorProfileRepository profileRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<AdminDeleteCreatorProfileCommandHandler> logger)
    : ICommandHandler<AdminDeleteCreatorProfileCommand>
{
    public async Task<Result> Handle(
        AdminDeleteCreatorProfileCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var profile = await profileRepository
                .GetByIdAsync(request.ProfileId, cancellationToken, asNoTracking: false)
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

            await cache.RemoveByTagAsync(ContentBlogsCacheKeys.CreatorProfileTag(profile.Id), cancellationToken).ConfigureAwait(false);
            await cache.RemoveByTagAsync(ContentBlogsCacheKeys.CreatorProfileUserTag(profile.UserId), cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Admin deleted CreatorProfile {ProfileId}. Reason: {Reason}", profile.Id, request.Reason);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "The request was cancelled."), Outcome.Canceled);
        }
    }
}
