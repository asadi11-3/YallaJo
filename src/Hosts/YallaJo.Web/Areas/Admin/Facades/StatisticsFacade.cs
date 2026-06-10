using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Home;
using YallaJo.Web.Areas.Admin.Models.Statistics;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class StatisticsFacade
{
    private readonly StatisticsApiClient _api;
    private readonly AdminToursApiClient _tours;
    private readonly PlacesApiClient _places;
    private readonly BlogsApiClient _blogs;
    private readonly GuidesApiClient _guides;
    private readonly UsersApiClient _users;

    public StatisticsFacade(
        StatisticsApiClient api,
        AdminToursApiClient tours,
        PlacesApiClient places,
        BlogsApiClient blogs,
        GuidesApiClient guides,
        UsersApiClient users)
    {
        _api = api;
        _tours = tours;
        _places = places;
        _blogs = blogs;
        _guides = guides;
        _users = users;
    }

    public async Task<ApiResult<StatisticsVm>> GetAnalyticsAsync(
        StatisticsFilterRequest request,
        CancellationToken ct = default)
    {
        // UI-UX-D1/R4: cap the page size at 50 (was 200).
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        if (request.UserId is { } userId && userId != Guid.Empty)
        {
            var userResult = await _api.GetUserInteractionsAsync(userId, pageSize, ct);
            if (userResult.IsUnauthorized)
            {
                return ApiResult<StatisticsVm>.ForceSignOut();
            }

            if (userResult is not { IsSuccess: true, Data: not null })
            {
                return ApiResult<StatisticsVm>.Fail(
                    userResult.StatusCode,
                    userResult.Error ?? "Could not load interactions for that user.");
            }

            var (entityNamesU, userNamesU) = await ResolveNamesAsync(userResult.Data, ct);
            return ApiResult<StatisticsVm>.Ok(
                StatisticsMapper.ToVm(userResult.Data, userId.ToString(), isUserLookup: true, entityNamesU, userNamesU));
        }

        var result = await _api.GetInteractionsAsync(
            request.InteractionType,
            request.EntityType,
            pageSize,
            ct);

        if (result.IsUnauthorized)
        {
            return ApiResult<StatisticsVm>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<StatisticsVm>.Fail(
                result.StatusCode,
                result.Error ?? "Could not load analytics.");
        }

        var (entityNames, userNames) = await ResolveNamesAsync(result.Data, ct);
        return ApiResult<StatisticsVm>.Ok(
            StatisticsMapper.ToVm(result.Data, entityNames: entityNames, userNames: userNames));
    }

    /// <summary>
    /// Resolves human-readable names for the distinct entity ids and user ids on the
    /// page so the view never renders a raw GUID (UI-UX rule F10). Resolution is
    /// concurrent (API1: Task.WhenAll over distinct ids — no N+1, R7) and bounded by
    /// the page size (≤200). Only entity types with a clean id→name GET are resolved
    /// (Tour/Place/Blog/Guide); Business/Creator have no such endpoint, so the view
    /// falls back to the localized entity-type label, still never a GUID. Failed or
    /// not-found lookups simply leave the name unresolved (graceful degradation).
    /// </summary>
    private async Task<(IReadOnlyDictionary<Guid, string> EntityNames, IReadOnlyDictionary<Guid, string> UserNames)>
        ResolveNamesAsync(InteractionPageResponse page, CancellationToken ct)
    {
        // Distinct (entityType, entityId) pairs, grouped per resolvable type.
        var byType = page.Items
            .GroupBy(i => i.EntityType, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(i => i.EntityId).Distinct().ToList(), StringComparer.OrdinalIgnoreCase);

        var entityTasks = new List<Task<KeyValuePair<Guid, string>?>>();

        void QueueEntity(string type, Func<Guid, Task<string?>> resolver)
        {
            if (!byType.TryGetValue(type, out var ids))
            {
                return;
            }

            foreach (var id in ids)
            {
                entityTasks.Add(ResolveOneAsync(id, resolver));
            }
        }

        QueueEntity("Tour", async id => (await _tours.GetByIdAsync(id, ct)) is { IsSuccess: true, Data: { } d } ? d.Name : null);
        QueueEntity("Place", async id => (await _places.GetByIdAsync(id, ct)) is { IsSuccess: true, Data: { } d } ? d.Name : null);
        QueueEntity("Blog", async id => (await _blogs.GetAdminByIdAsync(id, ct)) is { IsSuccess: true, Data: { } d } ? d.Title : null);
        QueueEntity("TourGuide", async id => (await _guides.GetGuideAsync(id, ct)) is { IsSuccess: true, Data: { } d } ? d.DisplayName : null);

        // Distinct non-empty user ids → email.
        var userIds = page.Items
            .Where(i => i.UserId is { } u && u != Guid.Empty)
            .Select(i => i.UserId!.Value)
            .Distinct()
            .ToList();

        var userTasks = userIds
            .Select(id => ResolveOneAsync(id, async uid =>
                (await _users.GetUserAsync(uid)) is { IsSuccess: true, Data: { } d } ? d.Email : null))
            .ToList();

        await Task.WhenAll(entityTasks.Concat(userTasks));

        var entityNames = entityTasks
            .Select(t => t.Result)
            .Where(kv => kv is not null)
            .Select(kv => kv!.Value)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var userNames = userTasks
            .Select(t => t.Result)
            .Where(kv => kv is not null)
            .Select(kv => kv!.Value)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        return (entityNames, userNames);
    }

    private static async Task<KeyValuePair<Guid, string>?> ResolveOneAsync(Guid id, Func<Guid, Task<string?>> resolver)
    {
        try
        {
            var name = await resolver(id);
            return string.IsNullOrWhiteSpace(name) ? null : new KeyValuePair<Guid, string>(id, name);
        }
        catch
        {
            // Name resolution is best-effort; a failure must not break analytics (F10 fallback handles display).
            return null;
        }
    }
}
