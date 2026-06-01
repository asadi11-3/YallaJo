using Accounts.Application.Caching;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Accounts.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Commands.UpdateMarketingConsent;

public sealed class UpdateMarketingConsentCommandHandler(
    IProfileRepository profileRepository,
    IAccountsUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    HybridCache cache)
    : ICommandHandler<UpdateMarketingConsentCommand, MarketingConsentResult>
{
    public async Task<Result<MarketingConsentResult>> Handle(
        UpdateMarketingConsentCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result<MarketingConsentResult>.Failure(
                Error.Unauthorized("Authentication is required."),
                Outcome.Unauthorized);
        }

        var userId = currentUser.UserId.Value;
        var profile = await profileRepository.FirstOrDefaultAsync(
            filter: p => p.UserId == userId,
            asNoTracking: false,
            ct: cancellationToken);

        if (profile is null)
        {
            return Result<MarketingConsentResult>.Failure(ProfileErrors.NotFound, Outcome.NotFound);
        }

        var consent = new MarketingConsent(
            request.EmailDigest,
            request.PushNotifications,
            request.ReEngagementCampaigns,
            DateTime.UtcNow);

        profile.UpdateMarketingConsent(consent);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<MarketingConsentResult>.Failure(
                new Error("Profile.ConcurrencyConflict", "The profile was modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        await cache.RemoveByTagAsync(AccountsCacheKeys.UserProfileTag(userId), cancellationToken);

        return Result<MarketingConsentResult>.Success(new MarketingConsentResult(
            consent.EmailDigest,
            consent.PushNotifications,
            consent.ReEngagementCampaigns,
            consent.LastUpdatedUtc));
    }
}
