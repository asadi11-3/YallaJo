namespace Auth.Application.Interfaces;

public interface IInviteLinkBuilder
{
    string Build(string email, string plainToken);
}
