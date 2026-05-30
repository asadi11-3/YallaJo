using YallaJo.SharedKernel.Domain.Exceptions;

namespace Auth.Domain.Entities;

/// <summary>
/// Thrown when a caller attempts to move an <see cref="ActivationToken"/>
/// through a transition that the state machine does not permit (for
/// example, consuming a revoked token or marking an already-consumed
/// token as delivered).
/// <para>
/// Escapes the aggregate as a programmer-error signal. Application
/// handlers should return <c>Result.Failure</c> for user-facing invalid
/// token conditions (expired, unknown, hash mismatch, attempt budget
/// exceeded) BEFORE invoking the domain verb — this exception exists as
/// the last-line invariant guard that catches bugs in the handler wiring.
/// </para>
/// </summary>
public sealed class InvalidActivationTokenTransitionException(
    ActivationTokenState from,
    ActivationTokenState to)
    : DomainException(
        code: "INVALID_ACTIVATION_TOKEN_TRANSITION",
        message: $"Activation token transition not allowed: {from} → {to}.")
{
    public ActivationTokenState From { get; } = from;
    public ActivationTokenState To   { get; } = to;
}
