using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Accounts.Models.Recommendations;

public sealed class RecommendationsVm
{
    public IReadOnlyList<RecommendationRowVm> Recommendations { get; set; } = [];
    public bool IsPersonalized { get; set; }
    public DateTime? ComputedAt { get; set; }
    public PreferencesFormVm Preferences { get; set; } = new();

    public bool HasRecommendations => Recommendations.Count > 0;
}

public sealed record SignalChipVm(string Title, string? Description);

public sealed record RecommendationRowVm(
    RecommendationEntityType Kind,
    Guid Id,
    string Name,
    string Slug,
    decimal? BasePrice,
    string? Currency,
    decimal AverageRating,
    int BookingCount,
    bool IsFeatured,
    bool IsPinned,
    bool IsBoosted,
    string? BadgeText,
    IReadOnlyList<SignalChipVm> Signals)
{
    public string KindLabel => Kind switch
    {
        RecommendationEntityType.Tour => "Tour",
        RecommendationEntityType.Place => "Place",
        RecommendationEntityType.Business => "Business",
        RecommendationEntityType.Category => "Category",
        _ => Kind.ToString(),
    };

    // Public detail link by entity kind.
    public string? DetailUrl => Kind switch
    {
        RecommendationEntityType.Tour => $"/tours/{Slug}",
        RecommendationEntityType.Place => $"/places/{Slug}",
        RecommendationEntityType.Business => $"/places/businesses/{Slug}",
        _ => null,
    };
}

public sealed class PreferencesFormVm
{
    [Display(Name = "Budget preference")]
    public string? BudgetTier { get; set; }

    [Display(Name = "Travelling with family")]
    public bool IsFamilyTraveler { get; set; }

    [Display(Name = "Current trip stage")]
    public string? CurrentTripStage { get; set; }

    // Budget options shown in the select (free string on the backend).
    public static IReadOnlyList<string> BudgetOptions { get; } = ["Budget", "Mid", "Luxury"];

    // Trip stage options mirror Analytics.Domain.Enums.TripStage names.
    public static IReadOnlyList<string> TripStageOptions { get; } =
        ["None", "Pre", "JustLanded", "MidTrip", "LastDay", "PostTrip"];

    public static string TripStageLabel(string stage) => stage switch
    {
        "None" => "Not set",
        "Pre" => "Planning the trip",
        "JustLanded" => "Just arrived",
        "MidTrip" => "Mid-trip",
        "LastDay" => "Last day",
        "PostTrip" => "After the trip",
        _ => stage,
    };
}
