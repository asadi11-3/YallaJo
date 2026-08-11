using Tracking.Application.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Tracking.Application.Queries.GetTrackingSessionDetails;

public sealed record GetTrackingSessionDetailsQuery(Guid SessionId)
    : IQuery<TrackingSessionDetailsDto>;
