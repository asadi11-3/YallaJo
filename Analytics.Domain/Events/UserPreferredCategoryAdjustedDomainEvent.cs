using YallaJo.SharedKernel.Domain.Event;

namespace Analytics.Domain.Events;

public sealed record UserPreferredCategoryAdjustedDomainEvent(
    Guid UserId,
    Guid CategoryId,
    decimal PreferenceScore) : DomainEventBase;
