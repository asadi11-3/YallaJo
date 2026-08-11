using Tracking.Application.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Tracking.Application.Commands.AddLocationSnapshot;

public sealed record AddLocationSnapshotCommand(
    Guid SessionId,
    decimal Latitude,
    decimal Longitude,
    double Accuracy,
    double? Speed,
    double? Heading,
    double? Altitude,
    DateTime CapturedAt) : ICommand<TrackingLocationSnapshotDto>;
