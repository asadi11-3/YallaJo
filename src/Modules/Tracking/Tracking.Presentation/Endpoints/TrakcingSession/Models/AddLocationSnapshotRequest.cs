using Tracking.Application.Commands.AddLocationSnapshot;

namespace Tracking.Presentation.Endpoints.TrakcingSession.Models;


public sealed record AddLocationSnapshotRequest(
    decimal Latitude,
    decimal Longitude,
    double Accuracy,
    double? Speed,
    double? Heading,
    double? Altitude,
    DateTime CapturedAt)
{
    public AddLocationSnapshotCommand ToCommand(Guid sessionId)
        => new(sessionId, Latitude, Longitude, Accuracy, Speed, Heading, Altitude, CapturedAt);
}
