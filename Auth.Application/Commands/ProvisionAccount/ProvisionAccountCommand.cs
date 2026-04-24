using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.ProvisionAccount;

/// <summary>
/// Phase 2B — admin-initiated account provisioning. Creates the Security
/// identity (in <c>Provisioned</c> lifecycle state) and the linked Accounts
/// profile shell. Does NOT issue an activation token and does NOT send an
/// activation email. Sending activation is a separate, explicit admin action
/// driven by <c>SendActivationEmailCommand</c>.
/// <para>
/// The legacy <c>InviteUserCommand</c> is kept as a façade that dispatches
/// this command followed by <c>SendActivationEmailCommand</c>, preserving
/// the existing endpoint contract while separating the two business intents
/// underneath.
/// </para>
/// </summary>
public sealed record ProvisionAccountCommand(
    string Email,
    string FirstName,
    string LastName,
    string? DisplayName,
    string? AvatarUrl,
    IReadOnlyList<Guid> InitialRoleIds) : ICommand<ProvisionAccountResult>;
