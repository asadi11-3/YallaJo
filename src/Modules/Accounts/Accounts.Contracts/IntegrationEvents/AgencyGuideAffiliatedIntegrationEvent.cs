using YallaJo.SharedKernel.Domain.Event;

namespace Accounts.Contracts.IntegrationEvents;

/// <summary>
/// Published (logical name: <c>accounts.agency.guide-affiliated.v1</c>) when an agency
/// affiliates a user as a tour guide. Consumers: Security (auto-assign TourGuide role).
/// Forward-declared — produced when Platform-Onboarding Agency Roster is implemented.
/// </summary>
public sealed record AgencyGuideAffiliatedIntegrationEvent(
    Guid UserId,
    Guid AgencyId,
    Guid AffiliationId) : IntegrationEventBase;
