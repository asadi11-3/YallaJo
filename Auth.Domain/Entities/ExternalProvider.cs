using YallaJo.SharedKernel.Domain.Entities;

namespace Auth.Domain.Entities;


public sealed class ExternalProvider : AuditableEntity, IAggregateRoot
{
    private ExternalProvider() { } // EF Core

    public Guid UserId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string ProviderUserId { get; private set; } = string.Empty;
    public string? ProviderEmail { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? VerifiedAt { get; private set; }

    public static ExternalProvider Create(
        Guid userId, string provider, string providerUserId, string? providerEmail = null)
    {
        return new ExternalProvider
        {
            UserId = userId,
            Provider = provider.Trim(),
            ProviderUserId = providerUserId,
            ProviderEmail = providerEmail?.Trim().ToLowerInvariant(),
            IsActive = true,
            VerifiedAt = DateTime.UtcNow
        };
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }
}
