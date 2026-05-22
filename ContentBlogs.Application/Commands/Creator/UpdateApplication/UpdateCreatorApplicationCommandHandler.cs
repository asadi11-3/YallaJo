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

namespace ContentBlogs.Application.Commands.Creator.UpdateApplication;

public sealed class UpdateCreatorApplicationCommandHandler(
    ICreatorApplicationRepository applicationRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<UpdateCreatorApplicationCommandHandler> logger)
    : ICommandHandler<UpdateCreatorApplicationCommand>
{
    public async Task<Result> Handle(
        UpdateCreatorApplicationCommand request,
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

            var application = await applicationRepository
                .GetByIdAsync(request.ApplicationId, cancellationToken)
                .ConfigureAwait(false);

            if (application is null)
            {
                return Result.Failure(
                    CreatorApplicationErrors.NotFound, Outcome.NotFound);
            }

            if (application.ApplicantUserId != currentUser.UserId.Value)
            {
                return Result.Failure(
                    new Error("Auth.Forbidden", "You can only update your own application."),
                    Outcome.Forbidden);
            }

            var resubmitResult = application.Resubmit(
                bio: request.Bio,
                portfolioUrls: request.PortfolioUrls,
                sampleWorkUrls: request.SampleWorkUrls,
                nicheIds: request.NicheIds,
                freeTags: request.FreeTags,
                languageIds: request.LanguageIds,
                preferredRegionIds: request.PreferredRegionIds,
                socialHandles: request.SocialHandles);

            if (resubmitResult.IsFailure)
            {
                return Result.Failure(resubmitResult.Errors.FirstOrDefault()!, Outcome.UnprocessableEntity);
            }

            applicationRepository.Update(application);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error("Creator.ConcurrencyConflict",
                        "Application was modified by another process."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorApplicationsListTag, cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorApplicationTag(request.ApplicationId), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "CreatorApplication updated/resubmitted: {ApplicationId}",
                request.ApplicationId);

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
