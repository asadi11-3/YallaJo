using Accounts.Application.Caching;
using Accounts.Domain.Enums;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Queries.Agency.GetAgencies;

public sealed class GetAgenciesQueryHandler(
    IProviderApplicationRepository providerApplicationRepository,
    HybridCache cache,
    ILogger<GetAgenciesQueryHandler> logger)
    : IQueryHandler<GetAgenciesQuery, GetAgenciesResult>
{
    public async Task<Result<GetAgenciesResult>> Handle(
        GetAgenciesQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"{AccountsCacheKeys.AgencyList}:p{request.Page}:s{request.PageSize}";

        var result = await cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                var agencies = await providerApplicationRepository.GetQueueAsync(
                    ProviderApplicationStatus.Approved,
                    ProviderType.Agency,
                    request.Page,
                    request.PageSize,
                    ct);

                var totalCount = await providerApplicationRepository.GetQueueCountAsync(
                    ProviderApplicationStatus.Approved,
                    ProviderType.Agency,
                    ct);

                var dtos = agencies.Select(a => new AgencyListItemDto(
                    a.UserId,
                    a.BusinessName,
                    a.ContactEmail,
                    a.Description,
                    a.ReviewedAt ?? a.CreatedAt)).ToList();

                return new GetAgenciesResult(dtos.AsReadOnly(), totalCount);
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(10) },
            tags: [AccountsCacheKeys.AgencyListTag],
            cancellationToken: cancellationToken);

        return Result<GetAgenciesResult>.Success(result!);
    }
}
