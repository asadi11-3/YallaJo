using ContentPlaces.Application.Queries.ServiceItem.Common;
using ContentPlaces.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Security.Contracts.Authorization;
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
        // IsElevated is computed at the endpoint and travels on the query so it can
        // participate in the cache key. Re-evaluate locally as defence-in-depth in case
        // this query is ever sent from a non-endpoint context. Either side may elevate;
        // neither side can strip elevation from a legitimately elevated caller.
        var isAdminTier = AppRoles.HighestPrivilegeLevel(currentUser.Roles)
            >= RolePrivilegeLevel.Admin;
        var business = await businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
        var isOwner = business is not null
                      && currentUser.UserId.HasValue
                      && business.OwnerId == currentUser.UserId.Value;
        var isElevated = request.IsElevated || isAdminTier || isOwner;

        Expression<Func<Domain.Entities.ServiceItem, bool>> filter = isElevated
            ? x => x.BusinessId == request.BusinessId
            : x => x.BusinessId == request.BusinessId && x.IsAvailable;

        var items = await serviceItemRepository.SelectAsync(
            selector: x => ServiceItemDto.From(x),
            filter:   filter,
            orderBy:  q => q.OrderBy(x => x.SortOrder).ThenBy(x => x.Name),
            ct:       cancellationToken);

        logger.LogInformation(
            "Fetched {Count} service items for Business {BusinessId} (elevated view: {Elevated})",
            items.Count, request.BusinessId, isElevated);

        return Result<IReadOnlyList<ServiceItemDto>>.Success(items);
    }
}
