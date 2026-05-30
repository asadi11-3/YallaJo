namespace Auth.Application.Commands.AdminReassignAccount;

/// <summary>
/// Phase 3C — result payload for <see cref="AdminReassignAccountCommand"/>.
/// Carries only an admin-facing confirmation message. The activation
/// email is dispatched out-of-band via the outbox pipeline.
/// </summary>
public sealed record AdminReassignAccountResult(string Message);
