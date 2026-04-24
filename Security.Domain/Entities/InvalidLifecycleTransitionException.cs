using YallaJo.SharedKernel.Domain.Exceptions;

namespace Security.Domain.Entities;

/// <summary>
/// Thrown when a caller attempts to move a <see cref="User"/> through a
/// transition that the lifecycle state machine does not permit (for example,
/// trying to <c>Activate</c> an <c>Archived</c> account, or to
/// <c>Suspend</c> an account that has not yet been activated).
/// <para>
/// Domain exception — escapes the aggregate as a programmer-error signal.
/// Application handlers should map invalid transitions to validation /
/// conflict failures via <c>Result.Failure</c> BEFORE invoking the domain
/// method; this exception exists as the last-line invariant guard.
/// </para>
/// </summary>
public sealed class InvalidLifecycleTransitionException(
    AccountLifecycleState from,
    AccountLifecycleState to)
    : DomainException(
        code: "INVALID_LIFECYCLE_TRANSITION",
        message: $"Account lifecycle transition not allowed: {from} → {to}.")
{
    public AccountLifecycleState From { get; } = from;
    public AccountLifecycleState To   { get; } = to;
}
