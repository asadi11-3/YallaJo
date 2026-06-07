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

/// <summary>
/// Cold-start onboarding quiz form (§3.6). The view posts the selected entity refs as
/// "Kind:EntityId" tokens (e.g. "Tour:3f2b…"). Bound as settable lists so MVC model
/// binding works (Oracle: do not use positional records for form-bound rows).
/// Validation: 1..20 total responses, no empty GUIDs (mirrors the API validator).
/// </summary>
public sealed class OnboardingFormVm : IValidatableObject
{
    /// <summary>Selected "interested" entity tokens, each "Kind:EntityId".</summary>
    public List<string> Interested { get; set; } = [];

    /// <summary>Selected "not interested" entity tokens, each "Kind:EntityId".</summary>
    public List<string> NotInterested { get; set; } = [];

    private const int MaxResponses = 20;

    /// <summary>Parses a "Kind:EntityId" token into a typed ref, or null if malformed/empty.</summary>
    public static (RecommendationEntityType Kind, Guid EntityId)? ParseToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var parts = token.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2
            || !Enum.TryParse<RecommendationEntityType>(parts[0], ignoreCase: true, out var kind)
            || !Guid.TryParse(parts[1], out var id)
            || id == Guid.Empty)
        {
            return null;
        }

        return (kind, id);
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var interested = Interested.Select(ParseToken).Where(r => r is not null).ToList();
        var notInterested = NotInterested.Select(ParseToken).Where(r => r is not null).ToList();
        var total = interested.Count + notInterested.Count;

        if (total < 1)
        {
            yield return new ValidationResult(
                "Pick at least one option to personalise your recommendations.",
                [nameof(Interested), nameof(NotInterested)]);
        }
        else if (total > MaxResponses)
        {
            yield return new ValidationResult(
                $"Please select no more than {MaxResponses} options in total.",
                [nameof(Interested), nameof(NotInterested)]);
        }

        // Malformed tokens (parsed to null but non-blank) are a binding/integrity error.
        if (Interested.Concat(NotInterested).Any(t => !string.IsNullOrWhiteSpace(t) && ParseToken(t) is null))
        {
            yield return new ValidationResult(
                "One or more selections were invalid. Please try again.",
                [nameof(Interested), nameof(NotInterested)]);
        }
    }
}
