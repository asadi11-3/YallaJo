using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Auth.Application.Commands.ActivateAccount;

/// <summary>
/// Phase 2B — user-initiated activation. Redeems the activation token sent
/// by the admin via <c>SendActivationEmailCommand</c>: verifies the token,
/// sets the initial password, verifies the primary email, and transitions
/// the account <c>PendingActivation → Active</c> in a single unit of work.
/// <para>
/// The legacy <c>AcceptInviteCommand</c> is kept as a façade over this
/// command so the existing anonymous activation endpoint keeps working
/// unchanged.
/// </para>
/// </summary>
public sealed record ActivateAccountCommand(
    string Email,
    string Token,
    string Password,
    string ConfirmPassword) : ICommand<ActivateAccountResult>;
