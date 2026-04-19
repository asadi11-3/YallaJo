namespace Auth.Application.Interfaces;

/// <summary>
/// Builds the invite-acceptance URL that is embedded in the invite email.
/// The template lives in configuration (Auth.Infrastructure) so Application
/// stays free of URL/hosting concerns.
/// </summary>
public interface IInviteLinkBuilder
{
    string Build(string email, string plainToken);
}
