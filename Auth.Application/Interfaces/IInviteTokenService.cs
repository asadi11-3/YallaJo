namespace Auth.Application.Interfaces;

public interface IInviteTokenService
{
    string Generate();

    string Hash(string plainToken);

    bool Verify(string plainToken, string hashedToken);
}
