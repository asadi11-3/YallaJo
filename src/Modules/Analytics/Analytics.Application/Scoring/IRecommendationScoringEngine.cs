using Analytics.Domain.Entities;
using Analytics.Domain.Enums;

namespace Analytics.Application.Scoring;

public interface IRecommendationScoringEngine
{
    IReadOnlyList<ScoredCandidate> Score(ScoringContext context);
}

public sealed record ScoringContext(
    EntityAttributeSnapshot Source,
    IReadOnlyList<EntityAttributeSnapshot> Candidates,
    SuggestionContext SuggestionContext,
    int MaxResults = 20,
    bool HalalOnly = false,
    string? AcceptLanguage = null,
    IReadOnlyDictionary<(EntityType Kind, Guid Id), BoostPackage>? ActiveBoosts = null,
    bool IsFamilyTraveler = false,
    IReadOnlyDictionary<Guid, decimal>? SeasonalityMultipliers = null,
    IReadOnlyList<HolidayBoostRule>? ActiveHolidayRules = null,
    int UserShareCount = 0);

/// <summary>Parsed holiday boost rule applied during scoring.</summary>
public sealed record HolidayBoostRule(string Filter, decimal Multiplier);

public sealed record ScoredCandidate(
    EntityAttributeSnapshot Snapshot,
    decimal Score,
    IReadOnlyList<string> Signals,
    bool IsBoosted = false);
