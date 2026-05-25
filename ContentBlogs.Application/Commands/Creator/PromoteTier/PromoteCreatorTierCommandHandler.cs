using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Creator.PromoteTier;

internal sealed class PromoteCreatorTierCommandHandler(
    ICreatorProfileRepository profileRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<PromoteCreatorTierCommandHandler> logger)
    : ICommandHandler<PromoteCreatorTierCommand>
{
    public async Task<Result> Handle(PromoteCreatorTierCommand request, CancellationToken cancellationToken)
    {
        var profile = await profileRepository.GetByIdAsync(request.ProfileId, cancellationToken, asNoTracking: false);
        if (profile is null)
            return Result.Failure(Domain.Errors.CreatorProfileErrors.NotFound, Outcome.NotFound);

        var result = profile.Promote(Guid.Empty, request.TargetTier, DateTime.UtcNow);
        if (!result.IsSuccess)
            return result;

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await cache.RemoveByTagAsync(Caching.ContentBlogsCacheKeys.CreatorProfileTag(profile.Id), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to promote creator profile {ProfileId}", request.ProfileId);
            return Result.Failure(Domain.Errors.CreatorProfileErrors.NotFound, Outcome.ServerError);
        }

        return Result.Success();
    }
}
