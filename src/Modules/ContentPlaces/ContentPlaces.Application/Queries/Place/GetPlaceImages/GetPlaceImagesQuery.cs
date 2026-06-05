using ContentPlaces.Application.Queries.Place.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Queries.Place.GetPlaceImages;

public sealed record GetPlaceImagesQuery(Guid PlaceId)
    : IQuery<IReadOnlyList<PlaceImageDto>>;
