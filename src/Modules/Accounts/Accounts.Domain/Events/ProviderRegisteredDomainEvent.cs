using Accounts.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Domain.Events;

public sealed record ProviderRegisteredDomainEvent(
    Guid ApplicationId,
    Guid UserId,
    ProviderType Type,
    DateTime RegisteredAt) : DomainEventBase;
