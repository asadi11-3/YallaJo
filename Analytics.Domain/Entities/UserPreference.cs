using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

public sealed class UserPreference : AuditableEntity, IAggregateRoot
{
    private UserPreference() { } // EF Core

    public Guid UserId { get; private set; }
    public string? BudgetTier { get; private set; }
    public bool IsFamilyTraveler { get; private set; }
    public string? CurrentTripStage { get; private set; }
    public DateTime? LastComputedAt { get; private set; }

    public static UserPreference Create(Guid userId, string? budgetTier, bool isFamilyTraveler, string? currentTripStage, DateTime? lastComputedAt)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id cannot be empty.", nameof(userId));

        var preference = new UserPreference { UserId = userId };
        preference.SetValues(budgetTier, isFamilyTraveler, currentTripStage, lastComputedAt);
        return preference;
    }

    public void Update(string? budgetTier, bool isFamilyTraveler, string? currentTripStage, DateTime? lastComputedAt)
    {
        SetValues(budgetTier, isFamilyTraveler, currentTripStage, lastComputedAt);
        MarkUpdated();
    }

    private void SetValues(string? budgetTier, bool isFamilyTraveler, string? currentTripStage, DateTime? lastComputedAt)
    {
        BudgetTier = string.IsNullOrWhiteSpace(budgetTier) ? null : budgetTier.Trim();
        IsFamilyTraveler = isFamilyTraveler;
        CurrentTripStage = string.IsNullOrWhiteSpace(currentTripStage) ? null : currentTripStage.Trim();
        LastComputedAt = lastComputedAt;
    }
}


