namespace Analytics.Application.Localization;

/// <summary>
/// Maps internal signal keys to user-facing labels.
/// V2 adds i18n-aware label resolution; for now returns English labels.
/// </summary>
public static class SignalLabels
{
    private static readonly IReadOnlyDictionary<string, SignalLabel> Labels = new Dictionary<string, SignalLabel>(StringComparer.OrdinalIgnoreCase)
    {
        ["nearby"] = new("Nearby", "Close to your location"),
        ["similar-price"] = new("Similar Price", "In a similar price range"),
        ["high-rating"] = new("Highly Rated", "Rated 4+ stars by travelers"),
        ["category-match"] = new("Similar Category", "Matches categories you enjoy"),
        ["popular"] = new("Popular", "Frequently booked by travelers"),
        ["popular-with-similar-users"] = new("Popular with Similar Users", "Other travelers like you also booked this"),
        ["matches-your-budget"] = new("Fits Your Budget", "Within your preferred price range"),
        ["family-friendly"] = new("Family Friendly", "Suitable for families with children"),
        ["recently-listed"] = new("Recently Listed", "A new addition to our catalog"),
        ["new-listing"] = new("New Listing", "Recently added"),
        ["featured"] = new("Featured", "Hand-picked by our team"),
        ["high-demand"] = new("High Demand", "Almost sold out — book soon"),
        ["boosted"] = new("Promoted", "Featured provider"),
        ["pinned"] = new("Editor's Choice", "Selected by our editors"),
        ["kid-friendly"] = new("Kid-Friendly", "Great for children"),
        ["seasonal"] = new("In Season", "Best time to visit this destination"),
        ["holiday-special"] = new("Holiday Special", "Perfect for the current holiday season"),
        ["photo-worthy"] = new("Photo-Worthy", "A stunning spot for photos"),
    };

    public static SignalLabel? GetLabel(string signalKey)
        => Labels.GetValueOrDefault(signalKey);

    public static IReadOnlyList<SignalLabelDto> Resolve(IReadOnlyList<string> signals)
        => signals
            .Select(s => Labels.TryGetValue(s, out var label)
                ? new SignalLabelDto(s, label.Title, label.Description)
                : new SignalLabelDto(s, s, null))
            .ToList();
}

public sealed record SignalLabel(string Title, string Description);
public sealed record SignalLabelDto(string Key, string Title, string? Description);
