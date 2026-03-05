namespace Analytics.Domain.Entities;

public sealed class UserPreferredCategory
{
    private UserPreferredCategory() { } // EF Core

    public Guid UserId { get; private set; }
    public Guid CategoryId { get; private set; }
    public decimal PreferenceScore { get; private set; }
}
