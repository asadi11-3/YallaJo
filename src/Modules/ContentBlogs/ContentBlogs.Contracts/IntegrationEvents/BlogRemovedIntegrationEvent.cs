using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents;

public sealed record BlogRemovedIntegrationEvent(
    Guid BlogId,
    string Slug,
    string Title,
    Guid AuthorId,
    Guid? AuthoredByCreatorProfileId,
    Guid RemovedByAdminId,
    string Reason,
    DateTime RemovedAt) : IntegrationEventBase;
