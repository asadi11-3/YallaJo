using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents;

public sealed record BlogRestoredIntegrationEvent(
    Guid BlogId,
    string Slug,
    DateTime RestoredAt) : IntegrationEventBase;
