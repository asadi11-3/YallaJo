using ContentPlaces.Application.Queries.ServiceItem.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.ServiceItem.ListServiceItems;

public sealed record ListServiceItemsQuery(Guid BusinessId)
    : IQuery<IReadOnlyList<ServiceItemDto>>;
