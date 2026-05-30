using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Domain.Events;

public sealed record TourProposalRejectedDomainEvent(
    Guid ProposalId,
    Guid TourGuideId,
    Guid GuideUserId,
    string Reason) : DomainEventBase;
