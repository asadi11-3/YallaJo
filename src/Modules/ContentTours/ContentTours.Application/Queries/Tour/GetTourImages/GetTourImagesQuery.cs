using ContentTours.Application.Queries.Tour.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Queries.Tour.GetTourImages;

public sealed record GetTourImagesQuery(Guid TourId)
    : IQuery<IReadOnlyList<TourImageDto>>;
