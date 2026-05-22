using Accounts.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Domain.Events;

public sealed record ProviderMoreDocsRequestedDomainEvent(
    Guid ApplicationId,
    Guid UserId,
    IReadOnlyList<DocumentType> MissingDocumentTypes,
    DateTime RequestedAt) : DomainEventBase;
