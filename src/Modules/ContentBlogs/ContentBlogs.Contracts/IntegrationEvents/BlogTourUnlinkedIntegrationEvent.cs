using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents;

public sealed record BlogTourUnlinkedIntegrationEvent(
    Guid BlogId,
    Guid TourId,
    DateTime UnlinkedAt) : IntegrationEventBase;
