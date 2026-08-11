using Tracking.Application.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Tracking.Application.Queries.GetLatestLocationByBookingId;

public sealed record GetLatestLocationByBookingIdQuery(Guid TourBookingId)
    : IQuery<TrackingLocationSnapshotDto>;
