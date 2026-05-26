using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Contracts;

public sealed record TourProposalRejectedIntegrationEvent(
    Guid ProposalId,
    Guid GuideUserId,
    string Reason) : IntegrationEventBase;
