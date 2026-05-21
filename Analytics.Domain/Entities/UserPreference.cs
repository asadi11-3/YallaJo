using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

[Obsolete("Out of scope for Phase 1. Reserved for Phase 3.")]
public sealed class UserPreference : AuditableEntity, IAggregateRoot
{
    private UserPreference() { } // EF Core

    public Guid UserId { get; private set; }
    public string PreferenceKey { get; private set; } = string.Empty;
    public string PreferenceValue { get; private set; } = string.Empty;
}


