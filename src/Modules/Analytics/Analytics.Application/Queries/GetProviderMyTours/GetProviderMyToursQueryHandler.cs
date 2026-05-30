using Analytics.Application.Interfaces;
using Analytics.Application.Models;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.GetProviderMyTours;

public sealed class GetProviderMyToursQueryHandler(IAnalyticsDashboardReader reader, ILogger<GetProviderMyToursQueryHandler> logger) : IQueryHandler<GetProviderMyToursQuery, CursorPageDto<ProviderTourListItemDto>>
{
    public async Task<Result<CursorPageDto<ProviderTourListItemDto>>> Handle(GetProviderMyToursQuery request, CancellationToken ct)
    {
        logger.LogDebug("Read provider tours {ProviderId}", request.ProviderId);
        return Result.Success(await reader.GetProviderToursAsync(request.ProviderId, request.AfterId, request.PageSize, ct));
    }
}
