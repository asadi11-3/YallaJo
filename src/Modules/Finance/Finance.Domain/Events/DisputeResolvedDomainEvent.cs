using Finance.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace Finance.Domain.Events;

public sealed record DisputeResolvedDomainEvent(
    Guid DisputeId,
    Guid PaymentId,
    Guid UserId,
    DisputeResolution Resolution,
    Guid ResolvedByAdminId,
    string? ResolutionNotes) : DomainEventBase;
