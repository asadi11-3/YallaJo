using Accounts.Application.Caching;
using Accounts.Domain.Enums;
using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Queries.Agency.GetAgencyDetail;

public sealed class GetAgencyDetailQueryHandler(
    IProviderApplicationRepository providerApplicationRepository,
    IAgencyAffiliationRepository agencyAffiliationRepository,
    HybridCache cache,
    ILogger<GetAgencyDetailQueryHandler> logger)
    : IQueryHandler<GetAgencyDetailQuery, AgencyDetailDto>
{
    public async Task<Result<AgencyDetailDto>> Handle(
        GetAgencyDetailQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"{AccountsCacheKeys.AgencyList}:detail:{request.AgencyUserId}";

        var result = await cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                var application = await providerApplicationRepository.GetByUserIdAsync(request.AgencyUserId, ct);
                if (application is null ||
                    application.Status != ProviderApplicationStatus.Approved ||
                    application.Type != ProviderType.Agency)
                    return null;

                var affiliations = await agencyAffiliationRepository.GetByAgencyUserIdAsync(
                    request.AgencyUserId, AgencyAffiliationStatus.Active, ct);
                var activeGuideCount = affiliations.Count;

                return new AgencyDetailDto(
                    application.UserId,
                    application.BusinessName,
                    application.ContactEmail,
                    application.ContactPhone,
                    application.Address,
                    application.Description,
                    application.Type,
                    activeGuideCount,
                    application.ReviewedAt ?? application.CreatedAt);
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(10) },
            tags: [AccountsCacheKeys.AgencyListTag],
            cancellationToken: cancellationToken);

        if (result is null)
        {
            logger.LogWarning("Agency detail requested for user {UserId} but no approved agency found", request.AgencyUserId);
            return Result<AgencyDetailDto>.Failure(ProviderApplicationErrors.NotFound, Outcome.NotFound);
        }

        return Result<AgencyDetailDto>.Success(result);
    }
}
