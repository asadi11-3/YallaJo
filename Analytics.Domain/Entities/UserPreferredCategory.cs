namespace Analytics.Domain.Entities;

[Obsolete("Out of scope for Phase 1. Reserved for Phase 3.")]
public sealed class UserPreferredCategory
{
    private UserPreferredCategory() { } // EF Core

    public Guid UserId { get; private set; }
    public Guid CategoryId { get; private set; }
    public decimal PreferenceScore { get; private set; }
}


