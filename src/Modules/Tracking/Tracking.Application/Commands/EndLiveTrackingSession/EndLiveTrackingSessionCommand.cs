using Tracking.Application.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Tracking.Application.Commands.EndLiveTrackingSession;

public sealed record EndLiveTrackingSessionCommand(
    Guid SessionId,
    string Reason,
    DateTime EndedAt) : ICommand<TrackingSessionSummaryDto>;
