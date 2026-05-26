using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Domain.Events;

public sealed record TourProposalSubmittedDomainEvent(
    Guid ProposalId,
    Guid TourGuideId,
    Guid GuideUserId,
    string Title) : DomainEventBase;
