using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record TourProposalApprovedIntegrationEvent(
    Guid ProposalId,
    Guid GuideUserId,
    Guid TourId,
    Guid ApprovedByUserId) : IntegrationEventBase;
