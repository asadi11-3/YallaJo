using Auth.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace Auth.Domain.Entities;


public sealed class Session : AuditableEntity, IAggregateRoot
{
    private Session() { } // EF Core

    public Guid UserId { get; private set; }
    public Guid DeviceId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? IpAddress { get; private set; }

    public static Session Create(Guid userId, Guid deviceId, DateTime expiresAt, string? ipAddress = null)
    {
        var session = new Session
        {
            UserId = userId,
            DeviceId = deviceId,
            ExpiresAt = expiresAt,
            IsRevoked = false,
            IpAddress = ipAddress
        };

        session.AddDomainEvent(new UserLoggedInEvent(userId, session.Id, deviceId, ipAddress ?? string.Empty));

        return session;
    }

    public void Revoke()
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        MarkUpdated();
        AddDomainEvent(new SessionRevokedEvent(UserId, Id));
    }
}
