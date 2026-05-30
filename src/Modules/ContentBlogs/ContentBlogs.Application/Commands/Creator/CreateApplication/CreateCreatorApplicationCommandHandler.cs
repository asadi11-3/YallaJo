using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Entities.Creators;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Errors;
using ContentBlogs.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Creator.CreateApplication;

public sealed class CreateCreatorApplicationCommandHandler(
    ICreatorApplicationRepository applicationRepository,
    ICreatorNicheRepository nicheRepository,
    IContentBlogsUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<CreateCreatorApplicationCommandHandler> logger)
    : ICommandHandler<CreateCreatorApplicationCommand, CreateCreatorApplicationResult>
{
    public async Task<Result<CreateCreatorApplicationResult>> Handle(
        CreateCreatorApplicationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
                        var userId = currentUser.UserId!.Value;

            // ── Guard: no active application ────────────────────────────────
            if (await applicationRepository
                .HasActiveApplicationAsync(userId, cancellationToken)
                .ConfigureAwait(false))
            {
                logger.LogWarning(
                    "CreateCreatorApplication rejected: user {UserId} already has an active application.",
                    userId);
                return Result.Failure<CreateCreatorApplicationResult>(
                    CreatorApplicationErrors.AlreadyHasActiveApplication,
                    Outcome.Conflict);
            }

            // ── Guard: reapplication count + cooling period ─────────────────
            var previousCount = await applicationRepository
                .CountByUserIdAsync(userId, cancellationToken)
                .ConfigureAwait(false);

            if (previousCount >= CreatorApplication.MaxReapplications)
            {
                logger.LogWarning(
                    "CreateCreatorApplication rejected: user {UserId} max reapplications reached ({Count}).",
                    userId, previousCount);
                return Result.Failure<CreateCreatorApplicationResult>(
                    CreatorApplicationErrors.MaxReapplicationsReached,
                    Outcome.UnprocessableEntity);
            }

            if (previousCount > 0)
            {
                var latest = await applicationRepository
                    .GetLatestByUserIdAsync(userId, cancellationToken)
                    .ConfigureAwait(false);

                if (latest is not null
                    && latest.Status is CreatorApplicationStatus.Rejected
                    && latest.UpdatedAt.HasValue
                    && (DateTime.UtcNow - latest.UpdatedAt.Value).TotalDays
                        < CreatorApplication.CoolingPeriodDays)
                {
                    logger.LogWarning(
                        "CreateCreatorApplication rejected: user {UserId} in cooling period.",
                        userId);
                    return Result.Failure<CreateCreatorApplicationResult>(
                        CreatorApplicationErrors.CoolingPeriodActive,
                        Outcome.UnprocessableEntity);
                }
            }

            // ── Validate niches exist ───────────────────────────────────────
            var nicheIds = request.NicheIds ?? [];
            if (nicheIds.Count > 0)
            {
                if (!await nicheRepository
                    .AllExistAndActiveAsync(nicheIds, cancellationToken)
                    .ConfigureAwait(false))
                {
                    return Result.Failure<CreateCreatorApplicationResult>(
                        new Error("Creator.NicheNotFound",
                            "One or more niche IDs are invalid or inactive."),
                        Outcome.UnprocessableEntity);
                }
            }

            // ── Build aggregate ─────────────────────────────────────────────
            var isReapplication = previousCount > 0;
            var applicationResult = isReapplication
                ? CreatorApplication.CreateReapplication(
                    applicantUserId: userId,
                    previousReapplicationCount: previousCount,
                    lastRejectedAt: (await applicationRepository
                        .GetLatestByUserIdAsync(userId, cancellationToken)
                        .ConfigureAwait(false))?.LastRejectedAt,
                    source: Domain.Enums.CreatorApplicationSource.SelfApplied,
                    invitationId: null,
                    bio: request.Bio,
                    portfolioUrls: request.PortfolioUrls ?? [],
                    sampleWorkUrls: request.SampleWorkUrls ?? [],
                    nicheIds: nicheIds,
                    freeTags: request.FreeTags ?? [],
                    languageIds: request.LanguageIds ?? [],
                    preferredRegionIds: request.PreferredRegionIds ?? [],
                    socialHandles: request.SocialHandles ?? new Dictionary<string, string>())
                : CreatorApplication.Create(
                    applicantUserId: userId,
                    source: Domain.Enums.CreatorApplicationSource.SelfApplied,
                    invitationId: null,
                    bio: request.Bio,
                    portfolioUrls: request.PortfolioUrls ?? [],
                    sampleWorkUrls: request.SampleWorkUrls ?? [],
                    nicheIds: nicheIds,
                    freeTags: request.FreeTags ?? [],
                    languageIds: request.LanguageIds ?? [],
                    preferredRegionIds: request.PreferredRegionIds ?? [],
                    socialHandles: request.SocialHandles ?? new Dictionary<string, string>());

            if (applicationResult.IsFailure)
            {
                return Result.Failure<CreateCreatorApplicationResult>(
                    applicationResult.Errors.FirstOrDefault()!, Outcome.UnprocessableEntity);
            }

            await applicationRepository
                .AddAsync(applicationResult.Value, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure<CreateCreatorApplicationResult>(
                    new Error("Creator.ConcurrencyConflict",
                        "Application was modified by another process. Please try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(
                ContentBlogsCacheKeys.CreatorApplicationsListTag, cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "CreatorApplication created: {ApplicationId} (UserId={UserId}, IsReapplication={IsReapply})",
                applicationResult.Value.Id, userId, isReapplication);

            return Result.Created(
                new CreateCreatorApplicationResult(applicationResult.Value.Id));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure<CreateCreatorApplicationResult>(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
