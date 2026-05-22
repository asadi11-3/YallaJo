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

namespace ContentBlogs.Application.Commands.Creator.SubmitApplication;

public sealed class SubmitCreatorApplicationCommandHandler(
    ICreatorApplicationRepository applicationRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<SubmitCreatorApplicationCommandHandler> logger)
    : ICommandHandler<SubmitCreatorApplicationCommand>
{
    public async Task<Result> Handle(
        SubmitCreatorApplicationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.UserId is null)
            {
                logger.LogWarning("SubmitCreatorApplication rejected: current user id missing.");
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
                    new Error("Auth.Forbidden", "You can only submit your own application."),
                    Outcome.Forbidden);
            }

            var submitResult = application.Submit();
            if (submitResult.IsFailure)
            {
                return Result.Failure(submitResult.Errors.FirstOrDefault()!, Outcome.UnprocessableEntity);
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
                        "Application was modified by another process. Please try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorApplicationsListTag, cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorApplicationTag(request.ApplicationId), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "CreatorApplication submitted: {ApplicationId} (UserId={UserId})",
                request.ApplicationId, currentUser.UserId.Value);

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
