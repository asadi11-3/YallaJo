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

namespace ContentBlogs.Application.Commands.Creator.RejectApplication;

public sealed class RejectCreatorApplicationCommandHandler(
    ICreatorApplicationRepository applicationRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<RejectCreatorApplicationCommandHandler> logger)
    : ICommandHandler<RejectCreatorApplicationCommand>
{
    public async Task<Result> Handle(
        RejectCreatorApplicationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
                        var application = await applicationRepository
                .GetByIdAsync(request.ApplicationId, cancellationToken)
                .ConfigureAwait(false);

            if (application is null)
            {
                return Result.Failure(
                    CreatorApplicationErrors.NotFound, Outcome.NotFound);
            }

            var rejectResult = application.Reject(currentUser.UserId!.Value, request.Reason);
            if (rejectResult.IsFailure)
            {
                return Result.Failure(rejectResult.Errors.FirstOrDefault()!, Outcome.UnprocessableEntity);
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
                        "Application was modified concurrently."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorApplicationsListTag, cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorApplicationTag(request.ApplicationId), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "CreatorApplication rejected: {ApplicationId} (AdminId={AdminId})",
                request.ApplicationId, currentUser.UserId!.Value);

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
