using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record TourApprovedIntegrationEvent(
    Guid TourId,
    Guid CreatedByUserId,
    Guid ApprovedByUserId,
    DateTime ApprovedAt,
    string Title = "",
    decimal BasePrice = 0m,
    string Currency = "JOD",
    bool IsInstantBooking = false,
    int MaxGroupSize = int.MaxValue) : IntegrationEventBase;
