namespace Auth.Application.Interfaces.SessionRevocation;

public interface ISessionRevocationService
{
    Task<SessionRevocationOutcome> RevokeAllForUserAsync(Guid userId, SessionRevocationReason reason, CancellationToken cancellationToken = default);
}
