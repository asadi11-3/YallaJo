namespace YallaJo.Web.Areas.Accounts.Models.Recommendations;

// Mirrors Analytics.Domain.Enums.EntityType (NOTE: 1-based).
public enum RecommendationEntityType : byte
{
    Tour = 1,
    Place = 2,
    Business = 3,
    Category = 4,
}

public sealed class RecommendationsResponse
{
    public IReadOnlyList<RecommendationItemResponse> Items { get; set; } = [];
    public bool IsPersonalized { get; set; }
    public DateTime? ComputedAt { get; set; }
}

public sealed class RecommendationItemResponse
{
    public RecommendationEntityType Kind { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public decimal? BasePrice { get; set; }
    public string? Currency { get; set; }
    public decimal AverageRating { get; set; }
    public int BookingCount { get; set; }
    public decimal Score { get; set; }
    public IReadOnlyList<string> Signals { get; set; } = [];
    public IReadOnlyList<SignalLabelResponse> SignalLabels { get; set; } = [];
    public bool IsFeatured { get; set; }
    public bool IsPinned { get; set; }
    public string? BadgeText { get; set; }
    public bool IsBoosted { get; set; }
}

public sealed class SignalLabelResponse
{
    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
}

public sealed class UserPreferencesResponse
{
    public Guid UserId { get; set; }
    public string? BudgetTier { get; set; }
    public bool IsFamilyTraveler { get; set; }
    public string? CurrentTripStage { get; set; }
    public DateTime? LastComputedAt { get; set; }
    public IReadOnlyList<UserPreferredCategoryResponse> Categories { get; set; } = [];
}

public sealed class UserPreferredCategoryResponse
{
    public Guid CategoryId { get; set; }
    public decimal PreferenceScore { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Request DTOs (mirror Analytics request records).
public sealed record UpdatePreferencesApiRequest(
    string? BudgetTier,
    bool IsFamilyTraveler,
    string? CurrentTripStage,
    IReadOnlyList<UpdatePreferredCategoryApiRequest> Categories);

public sealed record UpdatePreferredCategoryApiRequest(Guid CategoryId, decimal PreferenceScore);

public sealed record MarkNotInterestedApiRequest(RecommendationEntityType EntityKind, Guid EntityId);

public sealed record RecordInteractionApiRequest(
    Guid? UserId,
    string? SessionId,
    string EntityType,
    Guid EntityId,
    string InteractionType);
