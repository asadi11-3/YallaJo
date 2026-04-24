using Security.Domain.Entities;
using YallaJo.SharedKernel.Domain.Event;

namespace Security.Domain.Events;

/// <summary>
/// Raised when a <see cref="User"/>'s lifecycle state transitions to a new
/// value. Self-transitions (idempotent no-ops) do NOT raise the event.
/// <para>
/// Phase 2A: subscribers may consume this for telemetry / logging. Phase 3
/// will introduce the persistent lifecycle audit log subscriber that records
/// the transition with actor attribution.
/// </para>
/// </summary>
public sealed record AccountLifecycleTransitionedEvent(
    Guid UserId,
    AccountLifecycleState From,
    AccountLifecycleState To) : DomainEventBase;
