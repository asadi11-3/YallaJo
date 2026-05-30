using Analytics.Domain.Enums;

namespace Analytics.Application.Scoring;

/// <summary>
/// Holds per-context MMR lambda values. λ=1.0 means pure relevance, λ=0.0 means pure diversity.
/// </summary>
public static class SuggestionContextWeights
{
    public const decimal DefaultLambda = 0.7m;

    private static readonly IReadOnlyDictionary<SuggestionContext, decimal> LambdaByContext = new Dictionary<SuggestionContext, decimal>
    {
        [SuggestionContext.SimilarTours] = 0.7m,
        [SuggestionContext.SimilarBusinesses] = 0.7m,
        [SuggestionContext.SimilarHotels] = 0.7m,
        [SuggestionContext.AddAMeal] = 0.8m,          // less diversity needed for meal suggestions
        [SuggestionContext.AddAnActivity] = 0.75m,
        [SuggestionContext.WhereToStay] = 0.65m,       // more diversity for accommodation options
        [SuggestionContext.ExploreNearby] = 0.6m,       // high diversity for explore
        [SuggestionContext.PersonalizedFeed] = 0.5m,    // maximum diversity for feed
        [SuggestionContext.BecauseYouViewed] = 0.7m,
        [SuggestionContext.BecauseYouBooked] = 0.75m,
        [SuggestionContext.PostBookingAddOn] = 0.8m,
        [SuggestionContext.PostBookingFollowUp] = 0.7m,
    };

    public static decimal GetLambda(SuggestionContext context)
        => LambdaByContext.GetValueOrDefault(context, DefaultLambda);
}
