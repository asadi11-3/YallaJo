using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Creator.Posts.DemoteTier;

public sealed class DemoteCreatorTierCommandHandler(
    ICreatorProfileRepository profileRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<DemoteCreatorTierCommandHandler> logger)
    : ICommandHandler<DemoteCreatorTierCommand>
{
    public async Task<Result> Handle(
        DemoteCreatorTierCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
                        var profile = await profileRepository
                .GetByIdAsync(request.ProfileId, cancellationToken)
                .ConfigureAwait(false);

            if (profile is null)
            {
                return Result.Failure(CreatorProfileErrors.NotFound, Outcome.NotFound);
            }

            var demoteResult = profile.Demote(
                request.TargetTier,
                request.Reason,
                DateTime.UtcNow);

            if (demoteResult.IsFailure)
            {
                return Result.Failure(
                    demoteResult.Errors.FirstOrDefault()!,
                    Outcome.UnprocessableEntity);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorProfileTag(profile.Id), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Creator tier demoted: {ProfileId} to {TargetTier} by admin {AdminId}. Reason: {Reason}",
                profile.Id, request.TargetTier, currentUser.UserId!.Value, request.Reason);

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
