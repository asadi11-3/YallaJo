using System;

namespace ContentPlaces.Application.Features.ServiceItems.Queries.GetList;

public sealed record ServiceItemResponse(
    Guid Id,
    string Name,
    decimal Price,
    int DurationMinutes,
    string Currency
);
