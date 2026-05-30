using Accounts.Application.Commands.UpdateMarketingConsent;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Queries.GetMarketingConsent;

public sealed class GetMarketingConsentQueryHandler(
    IProfileRepository profileRepository,
    ICurrentUser currentUser)
    : IQueryHandler<GetMarketingConsentQuery, MarketingConsentResult>
{
    public async Task<Result<MarketingConsentResult>> Handle(
        GetMarketingConsentQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result<MarketingConsentResult>.Failure(
                Error.Unauthorized("Authentication is required."),
                Outcome.Unauthorized);
        }

        if (currentUser.UserId.Value != request.UserId)
        {
            return Result<MarketingConsentResult>.Failure(
                Error.Forbidden("You may only access your own marketing consent."),
                Outcome.Forbidden);
        }

        var profile = await profileRepository.FirstOrDefaultAsync(
            filter: p => p.UserId == request.UserId,
            ct: cancellationToken);

        if (profile is null)
        {
            return Result<MarketingConsentResult>.Failure(ProfileErrors.NotFound, Outcome.NotFound);
        }

        var consent = profile.MarketingConsent;

        return Result<MarketingConsentResult>.Success(new MarketingConsentResult(
            consent?.EmailDigest ?? false,
            consent?.PushNotifications ?? false,
            consent?.ReEngagementCampaigns ?? false,
            consent?.LastUpdatedUtc));
    }
}
