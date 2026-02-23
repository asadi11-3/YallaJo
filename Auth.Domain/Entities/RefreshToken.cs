using YallaJo.SharedKernel.Domain.Entities;

namespace Auth.Domain.Entities;

/// <summary>
/// Hashed refresh token for token rotation.
/// UserId references Security.User.Id (no FK, cross-DB).
/// </summary>
public sealed class RefreshToken : AuditableEntity, IAggregateRoot
{
    private RefreshToken() { } // EF Core

    public Guid UserId { get; private set; }
    public Guid SessionId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }

    public static RefreshToken Create(Guid userId, Guid sessionId, string tokenHash, DateTime expiresAt)
    {
        return new RefreshToken
        {
            UserId = userId,
            SessionId = sessionId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            IsRevoked = false
        };
    }

    public void Revoke(Guid? replacedByTokenId = null)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        ReplacedByTokenId = replacedByTokenId;
        MarkUpdated();
    }
}
