using ContentPlaces.Application.Queries.Place.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.Place.GetPlaceById;

public sealed record GetPlaceByIdQuery(Guid PlaceId) : IQuery<PlaceDetailDto>;
