using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Commands.Creator.Common;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Entities.Creators;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Creator.ApproveApplication;

public sealed class ApproveCreatorApplicationCommandHandler(
    ICreatorApplicationRepository applicationRepository,
    ICreatorProfileRepository profileRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<ApproveCreatorApplicationCommandHandler> logger)
    : ICommandHandler<ApproveCreatorApplicationCommand, ApproveCreatorApplicationResult>
{
    public async Task<Result<ApproveCreatorApplicationResult>> Handle(
        ApproveCreatorApplicationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (currentUser.UserId is null)
            {
                return Result.Failure<ApproveCreatorApplicationResult>(
                    new Error("Auth.UserIdMissing", "Authenticated user id is missing."),
                    Outcome.Unauthorized);
            }

            var adminId = currentUser.UserId.Value;

            var application = await applicationRepository
                .GetByIdAsync(request.ApplicationId, cancellationToken)
                .ConfigureAwait(false);

            if (application is null)
            {
                return Result.Failure<ApproveCreatorApplicationResult>(
                    CreatorApplicationErrors.NotFound, Outcome.NotFound);
            }

            var approveResult = application.Approve(adminId);
            if (approveResult.IsFailure)
            {
                return Result.Failure<ApproveCreatorApplicationResult>(
                    approveResult.Errors.FirstOrDefault()!, Outcome.UnprocessableEntity);
            }

            // ── Create Creator Profile ──────────────────────────────────────
            var existingSlugs = await profileRepository
                .GetAllSlugsAsync(cancellationToken)
                .ConfigureAwait(false);

            var slug = CreatorSlugGenerator.Generate(
                request.DisplayName, existingSlugs);

            var profileResult = CreatorProfile.Create(
                userId: application.ApplicantUserId,
                applicationId: application.Id,
                slug: slug,
                displayName: request.DisplayName,
                bio: application.Bio,
                avatarUrl: request.AvatarUrl);

            if (profileResult.IsFailure)
            {
                return Result.Failure<ApproveCreatorApplicationResult>(
                    profileResult.Errors.FirstOrDefault()!, Outcome.UnprocessableEntity);
            }

            applicationRepository.Update(application);
            await profileRepository
                .AddAsync(profileResult.Value, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure<ApproveCreatorApplicationResult>(
                    new Error("Creator.ConcurrencyConflict",
                        "Application was modified concurrently. Please try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorApplicationsListTag, cancellationToken)
                .ConfigureAwait(false);
            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorApplicationTag(request.ApplicationId), cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "CreatorApplication approved: {ApplicationId} → Profile {ProfileId} (AdminId={AdminId})",
                request.ApplicationId, profileResult.Value.Id, adminId);

            return Result.Created(new ApproveCreatorApplicationResult(
                request.ApplicationId, profileResult.Value.Id));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<ApproveCreatorApplicationResult>(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
