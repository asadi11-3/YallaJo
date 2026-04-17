using MediatR;
using System;

namespace ContentPlaces.Application.Features.ServiceItems.Queries.GetById;

public sealed record ServiceItemDetailResponse(
    Guid Id,
    string Name,
    decimal Price,
    int DurationMinutes,
    int MaxCapacity,
    string Currency,
    int SortOrder
);

public sealed record GetServiceItemByIdQuery(Guid BusinessId, Guid ServiceItemId)
    : IRequest<ServiceItemDetailResponse>;
