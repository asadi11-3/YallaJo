namespace Analytics.Domain.Entities;

public sealed class UserPreferredCategory
{
    private UserPreferredCategory() { } // EF Core

    public Guid UserId { get; private set; }
    public Guid CategoryId { get; private set; }
    public decimal PreferenceScore { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static UserPreferredCategory Create(Guid userId, Guid categoryId, decimal score)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id cannot be empty.", nameof(userId));

        if (categoryId == Guid.Empty)
            throw new ArgumentException("Category id cannot be empty.", nameof(categoryId));

        return new UserPreferredCategory
        {
            UserId = userId,
            CategoryId = categoryId,
            PreferenceScore = score,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateScore(decimal newScore)
    {
        PreferenceScore = newScore;
        UpdatedAt = DateTime.UtcNow;
    }
}


