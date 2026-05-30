using Accounts.Domain.Enums;
using Accounts.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Queries.Agency.GetAvailableGuides;

public sealed class GetAvailableGuidesQueryHandler(
    IProviderApplicationRepository providerApplicationRepository,
    IAgencyAffiliationRepository agencyAffiliationRepository,
    ILogger<GetAvailableGuidesQueryHandler> logger)
    : IQueryHandler<GetAvailableGuidesQuery, IReadOnlyList<AvailableGuideDto>>
{
    public async Task<Result<IReadOnlyList<AvailableGuideDto>>> Handle(
        GetAvailableGuidesQuery request,
        CancellationToken cancellationToken)
    {
        // Get all IndependentGuide approved providers
        var guides = await providerApplicationRepository.GetQueueAsync(
            ProviderApplicationStatus.Approved,
            ProviderType.IndependentGuide,
            request.Page,
            request.PageSize,
            cancellationToken);

        // Filter out those already affiliated
        var result = new List<AvailableGuideDto>();
        foreach (var guide in guides)
        {
            var isAffiliated = await agencyAffiliationRepository.IsGuideAffiliatedAsync(guide.UserId, cancellationToken);
            if (!isAffiliated)
                result.Add(new AvailableGuideDto(guide.UserId, guide.BusinessName, guide.ContactEmail, guide.CreatedAt));
        }

        return Result<IReadOnlyList<AvailableGuideDto>>.Success(result.AsReadOnly());
    }
}
