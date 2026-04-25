using ContentPlaces.Application.Queries.ServiceItem.Common;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Application.Queries.ServiceItem.ListServiceItems;

public sealed class ListServiceItemsQueryHandler(
    IServiceItemRepository serviceItemRepository,
    IBusinessRepository businessRepository,
    ICurrentUser currentUser,
    ILogger<ListServiceItemsQueryHandler> logger)
    : IQueryHandler<ListServiceItemsQuery, IReadOnlyList<ServiceItemDto>>
{
    public async Task<Result<IReadOnlyList<ServiceItemDto>>> Handle(
        ListServiceItemsQuery request,
        CancellationToken cancellationToken)
    {
        // Determine visibility: owner and admin see all items; public sees IsAvailable only.
        var business = await businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
        var isAdmin  = currentUser.IsInRole("Admin");
        var isOwner  = business is not null
                       && currentUser.UserId.HasValue
                       && business.OwnerId == currentUser.UserId.Value;

        Expression<Func<Domain.Entities.ServiceItem, bool>> filter = (isAdmin || isOwner)
            ? x => x.BusinessId == request.BusinessId
            : x => x.BusinessId == request.BusinessId && x.IsAvailable;

        var items = await serviceItemRepository.SelectAsync(
            selector: x => ServiceItemDto.From(x),
            filter:   filter,
            orderBy:  q => q.OrderBy(x => x.SortOrder).ThenBy(x => x.Name),
            ct:       cancellationToken);

        logger.LogInformation(
            "Fetched {Count} service items for Business {BusinessId} (owner/admin view: {Elevated})",
            items.Count, request.BusinessId, isAdmin || isOwner);

        return Result<IReadOnlyList<ServiceItemDto>>.Success(items);
    }
}
