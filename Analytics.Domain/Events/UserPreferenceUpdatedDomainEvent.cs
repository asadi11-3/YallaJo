using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Domain.Events;

public sealed record UserPreferenceUpdatedDomainEvent(
    Guid PreferenceId,
    Guid UserId,
    string PreferenceKey) : DomainEventBase;
