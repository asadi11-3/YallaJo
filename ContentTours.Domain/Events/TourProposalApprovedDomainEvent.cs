using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Domain.Events;

public sealed record TourProposalApprovedDomainEvent(
    Guid ProposalId,
    Guid TourGuideId,
    Guid GuideUserId,
    Guid CreatedTourId,
    Guid ApprovedByAdminId) : DomainEventBase;
