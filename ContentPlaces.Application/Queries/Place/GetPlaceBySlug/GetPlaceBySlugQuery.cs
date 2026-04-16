using ContentPlaces.Application.Queries.Place.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.Place.GetPlaceBySlug;

public sealed record GetPlaceBySlugQuery(string Slug) : IQuery<PlaceDetailDto>;
