using Analytics.Application.Models;
using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Queries.GetEntitySuggestions;

public sealed record GetEntitySuggestionsQuery(
    EntityType SourceKind,
    Guid SourceId,
    SuggestionContext Context,
    int Limit = 10,
    string? Language = null,
    bool HalalOnly = false,
    string? AcceptLanguage = null) : IQuery<EntitySuggestionsResponse>, ICacheableQuery
{
    public string CacheKey => $"ct:analytics:recs:{SourceKind}:{SourceId:N}:ctx:{Context}:lang:{Language ?? "en"}:limit:{Math.Clamp(Limit, 1, 100)}:halal:{HalalOnly}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
    public IReadOnlyList<string> Tags => [$"analytics:recs:{SourceKind}:{SourceId:N}"];
}
