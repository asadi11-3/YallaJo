namespace Auth.Application.Commands.ExternalLogin;

/// <summary>
/// Which internal resolver branch produced an
/// <see cref="AutoLinkOutcome"/>. Emitted on refusal log lines so operators
/// can triage WHY a sign-in returned the generic "invalid or expired" result
/// without the client ever seeing that detail.
/// </summary>
internal enum AutoLinkPath
{
    Unresolved = 0,
    ExistingLink,
    AutoLink,
    AutoCreate,
}
