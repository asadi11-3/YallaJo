using Tracking.Application.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Tracking.Application.Queries.GetTourGuideActiveSessions;

public sealed record GetTourGuideActiveSessionsQuery(Guid TourGuideId)
    : IQuery<IReadOnlyList<TrackingSessionSummaryDto>>;
