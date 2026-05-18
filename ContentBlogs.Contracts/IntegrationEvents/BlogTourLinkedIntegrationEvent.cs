using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents;

public sealed record BlogTourLinkedIntegrationEvent(
    Guid BlogId,
    Guid TourId,
    DateTime LinkedAt) : IntegrationEventBase;
