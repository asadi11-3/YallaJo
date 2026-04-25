namespace Auth.Application.Commands.ExternalLogin;

internal enum AutoLinkPath
{
    Unresolved = 0,
    ExistingLink,
    AutoLink,
    AutoCreate,
}
