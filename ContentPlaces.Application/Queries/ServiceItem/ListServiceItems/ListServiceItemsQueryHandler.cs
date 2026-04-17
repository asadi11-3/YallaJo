using ContentPlaces.Application.Queries.ServiceItem.Common;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.ServiceItem.ListServiceItems;

public sealed class ListServiceItemsQueryHandler(
    IServiceItemRepository serviceItemRepository,
    ILogger<ListServiceItemsQueryHandler> logger)
    : IQueryHandler<ListServiceItemsQuery, IReadOnlyList<ServiceItemDto>>
{
    public async Task<Result<IReadOnlyList<ServiceItemDto>>> Handle(
        ListServiceItemsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await serviceItemRepository.SelectAsync(
            selector: x => ServiceItemDto.From(x),
            filter: x => x.BusinessId == request.BusinessId,
            orderBy: q => q.OrderBy(x => x.SortOrder).ThenBy(x => x.Name),
            ct: cancellationToken);

        logger.LogInformation(
            "Fetched {Count} service items for Business {BusinessId}",
            items.Count,
            request.BusinessId);

        return Result<IReadOnlyList<ServiceItemDto>>.Success(items);
    }
}
