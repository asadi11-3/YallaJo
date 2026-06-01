using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Creator.DemoteTier;

internal sealed class DemoteCreatorTierCommandHandler(
    ICreatorProfileRepository profileRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<DemoteCreatorTierCommandHandler> logger)
    : ICommandHandler<DemoteCreatorTierCommand>
{
    public async Task<Result> Handle(DemoteCreatorTierCommand request, CancellationToken cancellationToken)
    {
        var profile = await profileRepository.GetByIdAsync(request.ProfileId, cancellationToken, asNoTracking: false);
        if (profile is null)
            return Result.Failure(Domain.Errors.CreatorProfileErrors.NotFound, Outcome.NotFound);

        var result = profile.Demote(request.TargetTier, request.Reason, DateTime.UtcNow);
        if (!result.IsSuccess)
            return result;

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await cache.RemoveByTagAsync(Caching.ContentBlogsCacheKeys.CreatorProfileTag(profile.Id), cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(
                new Error("Creator.ConcurrencyConflict", "Profile was modified concurrently."),
                Outcome.Conflict);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to demote creator profile {ProfileId}", request.ProfileId);
            return Result.Failure(Domain.Errors.CreatorProfileErrors.NotFound, Outcome.ServerError);
        }

        return Result.Success();
    }
}
