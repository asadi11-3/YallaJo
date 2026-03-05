using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

public sealed class UserPreference : AuditableEntity
{
    private UserPreference() { } // EF Core

    public Guid UserId { get; private set; }
    public string PreferenceKey { get; private set; } = string.Empty;
    public string PreferenceValue { get; private set; } = string.Empty;
}
