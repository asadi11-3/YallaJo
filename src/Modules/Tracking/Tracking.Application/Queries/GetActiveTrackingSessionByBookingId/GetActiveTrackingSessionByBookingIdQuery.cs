using Tracking.Application.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Tracking.Application.Queries.GetActiveTrackingSessionByBookingId;

public sealed record GetActiveTrackingSessionByBookingIdQuery(Guid TourBookingId)
    : IQuery<TrackingSessionDetailsDto>;
