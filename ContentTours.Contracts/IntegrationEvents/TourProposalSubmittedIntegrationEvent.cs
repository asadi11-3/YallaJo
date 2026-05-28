using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record TourProposalSubmittedIntegrationEvent(
    Guid ProposalId,
    Guid GuideUserId,
    string Title) : IntegrationEventBase;
