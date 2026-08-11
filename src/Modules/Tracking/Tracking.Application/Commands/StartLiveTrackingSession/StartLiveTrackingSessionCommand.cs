using Tracking.Application.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Tracking.Application.Commands.StartLiveTrackingSession;

public sealed record StartLiveTrackingSessionCommand(
    Guid UserId,
    Guid TourBookingId,
    Guid TourGuideId,
    DateTime StartedAt,
    IReadOnlyList<Guid>? WaypointIds) : ICommand<TrackingSessionSummaryDto>;
