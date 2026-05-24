using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Creator.Posts.PromoteTier;

public sealed class PromoteCreatorTierCommandHandler(
    ICreatorProfileRepository profileRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<PromoteCreatorTierCommandHandler> logger)
    : ICommandHandler<PromoteCreatorTierCommand>
{
    public async Task<Result> Handle(
        PromoteCreatorTierCommand request,
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

            var promoteResult = profile.Promote(
                currentUser.UserId!.Value,
                request.TargetTier,
                DateTime.UtcNow);

            if (promoteResult.IsFailure)
            {
                return Result.Failure(
                    promoteResult.Errors.FirstOrDefault()!,
                    Outcome.UnprocessableEntity);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorProfileTag(profile.Id), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Creator tier promoted: {ProfileId} to {TargetTier} by admin {AdminId}",
                profile.Id, request.TargetTier, currentUser.UserId!.Value);

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
