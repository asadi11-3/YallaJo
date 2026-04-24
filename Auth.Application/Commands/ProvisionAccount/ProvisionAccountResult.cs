namespace Auth.Application.Commands.ProvisionAccount;

/// <summary>
/// Result payload returned on successful account provisioning. Exposes the
/// newly created Security user id and Accounts profile id so the caller
/// (and, transitively, the legacy <c>InviteUser</c> façade) can surface both
/// to admin UIs or trigger follow-on actions such as dispatching the
/// activation email.
/// </summary>
public sealed record ProvisionAccountResult(Guid UserId, Guid ProfileId);
