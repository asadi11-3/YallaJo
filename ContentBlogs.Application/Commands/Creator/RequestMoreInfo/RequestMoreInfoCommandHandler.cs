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

namespace ContentBlogs.Application.Commands.Creator.RequestMoreInfo;

public sealed class RequestMoreInfoCommandHandler(
    ICreatorApplicationRepository applicationRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<RequestMoreInfoCommandHandler> logger)
    : ICommandHandler<RequestMoreInfoCommand>
{
    public async Task<Result> Handle(
        RequestMoreInfoCommand request,
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

            var moreInfoResult = application.RequestMoreInfo(
                currentUser.UserId.Value, request.AdminNote);

            if (moreInfoResult.IsFailure)
            {
                return Result.Failure(moreInfoResult.Errors.FirstOrDefault()!, Outcome.UnprocessableEntity);
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
                "CreatorApplication more info requested: {ApplicationId} (AdminId={AdminId})",
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
