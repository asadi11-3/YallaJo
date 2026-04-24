using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.AdminReassignAccount;

/// <summary>
/// Phase 3C — admin-initiated account reassignment. Retargets an
/// existing account to a new email/new real user:
/// <list type="bullet">
///   <item><description>Primary email changes to <paramref name="NewEmail"/> (unverified).</description></item>
///   <item><description>Password is replaced with an unusable placeholder (fail-closed).</description></item>
///   <item><description>Lifecycle moves to <c>PendingActivation</c>.</description></item>
///   <item><description>Sessions + refresh tokens revoked (<c>AccountReassigned</c>).</description></item>
///   <item><description>Outstanding activation + password-reset tokens superseded.</description></item>
///   <item><description>Active external provider links deactivated.</description></item>
///   <item><description>Fresh activation token issued and emailed via outbox.</description></item>
/// </list>
/// <para>
/// The admin actor is resolved server-side from <c>ICurrentUser</c>, so
/// this command never carries an actor id — the
/// admin-cannot-forge-actor invariant is preserved.
/// </para>
/// </summary>
public sealed record AdminReassignAccountCommand(
    Guid TargetUserId,
    string NewEmail,
    string? Reason) : ICommand<AdminReassignAccountResult>;
