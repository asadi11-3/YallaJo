using YallaJo.SharedKernel.Domain.Exceptions;

namespace Security.Domain.Entities;

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
