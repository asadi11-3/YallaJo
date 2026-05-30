using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents;

public sealed record BlogRejectedIntegrationEvent(
    Guid BlogId,
    string Slug,
    string Title,
    Guid AuthorId,
    Guid? AuthoredByCreatorProfileId,
    Guid RejectedByAdminId,
    string Reason,
    DateTime RejectedAt) : IntegrationEventBase;
