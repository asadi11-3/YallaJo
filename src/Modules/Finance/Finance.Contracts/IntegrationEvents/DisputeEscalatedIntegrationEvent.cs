using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Contracts.IntegrationEvents;

/// <summary>
/// Published (logical name: finance.dispute-escalated.v1) when an admin escalates a dispute
/// (e.g. to senior support or arbitration). Consumers: Messaging (notify the user that their
/// dispute is receiving higher-tier attention).
/// </summary>
/// <param name="DisputeId">The dispute aggregate id.</param>
/// <param name="PaymentId">Payment this dispute is attached to.</param>
/// <param name="UserId">Owner of the disputed payment. May be Guid.Empty for pre-enrichment in-flight messages.</param>
/// <param name="Reason">Escalation reason supplied by the escalating admin.</param>
/// <param name="EscalatedByAdminId">Admin user who escalated the dispute.</param>
public sealed record DisputeEscalatedIntegrationEvent(
    Guid DisputeId,
    Guid PaymentId,
    Guid UserId,
    string Reason,
    Guid EscalatedByAdminId) : IntegrationEventBase;
