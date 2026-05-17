using YallaJo.SharedKernel.Domain.Event;

namespace Social.Domain.Events;

public sealed record ReviewAutoHiddenDomainEvent(Guid ReviewId, int ReportCount) : DomainEventBase;
