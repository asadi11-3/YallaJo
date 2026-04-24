using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.SendActivationEmail;

/// <summary>
/// Phase 2B — explicit admin action that issues a fresh activation token
/// for an account currently in <c>Provisioned</c> or <c>PendingActivation</c>
/// state, sends the activation email, and transitions the account to
/// <c>PendingActivation</c> on successful dispatch.
/// <para>
/// Refuses accounts that are past the activation window (Active, Suspended,
/// PendingPasswordReset, Archived) with a <c>Conflict</c> outcome. Safe to
/// call repeatedly on a <c>PendingActivation</c> account — prior unused
/// invite tokens are marked used before a new one is issued.
/// </para>
/// <para>
/// Used directly by the new admin UI and indirectly by the legacy
/// <c>InviteUserCommand</c> and <c>ResendInviteCommand</c> façades.
/// </para>
/// </summary>
public sealed record SendActivationEmailCommand(string Email)
    : ICommand<SendActivationEmailResult>;
