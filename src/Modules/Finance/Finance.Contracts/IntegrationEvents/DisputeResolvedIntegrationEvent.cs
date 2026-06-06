using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Contracts.IntegrationEvents;

/// <summary>
/// Published (logical name: finance.dispute-resolved.v1) when a payment-level dispute is resolved by an admin.
/// Consumers: Messaging (notify the user of the resolution).
/// Phase-3 WS-4: adds Resolution as string so Messaging doesn't need a Finance.Domain.Enums dependency.
/// </summary>
/// <param name="DisputeId">The dispute aggregate id.</param>
/// <param name="PaymentId">Payment this dispute is attached to.</param>
/// <param name="UserId">Owner of the disputed payment. May be Guid.Empty for pre-enrichment in-flight messages; consumers MUST tolerate and skip user-targeted notifications in that case.</param>
/// <param name="Resolution">String form of <c>DisputeResolution</c> (e.g. RefundFull, RefundPartial, NoRefund, Compromise).</param>
/// <param name="ResolvedByAdminId">Admin user who resolved the dispute.</param>
/// <param name="ResolutionNotes">Optional human-readable resolution notes.</param>
public sealed record DisputeResolvedIntegrationEvent(
    Guid DisputeId,
    Guid PaymentId,
    Guid UserId,
    string Resolution,
    Guid ResolvedByAdminId,
    string? ResolutionNotes) : IntegrationEventBase;
