using Accounts.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Domain.Events;

public sealed record ProviderApprovedDomainEvent(
    Guid ApplicationId,
    Guid UserId,
    ProviderType Type,
    DateTime ApprovedAt,
    Guid ApprovedByAdminId) : DomainEventBase;
