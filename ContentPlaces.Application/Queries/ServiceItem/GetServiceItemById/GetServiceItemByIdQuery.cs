using ContentPlaces.Application.Queries.ServiceItem.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.ServiceItem.GetServiceItemById;

public sealed record GetServiceItemByIdQuery(Guid BusinessId, Guid ServiceItemId)
    : IQuery<ServiceItemDetailDto>;
