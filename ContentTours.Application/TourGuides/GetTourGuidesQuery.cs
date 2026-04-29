using System;
using System.Collections.Generic;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Features.TourGuides.Queries.GetByTourId;

public sealed record TourGuideResponse(
    Guid TourGuideId,
    bool IsPrimary
);

public sealed record GetTourGuidesQuery(Guid TourId)
    : IQuery<IReadOnlyCollection<TourGuideResponse>>;
