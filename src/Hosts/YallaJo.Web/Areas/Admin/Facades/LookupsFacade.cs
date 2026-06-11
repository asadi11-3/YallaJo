using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Lookups;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

/// <summary>
/// Backs the admin typeahead lookup proxies (F10). The browser only ever talks to
/// the MVC proxy actions (JS5); this facade maps module suggest responses into the
/// normalized <see cref="LookupItemVm"/> shape consumed by admin-lookup.js.
/// </summary>
public sealed class LookupsFacade(UsersApiClient users, PlacesApiClient places)
{
    private readonly UsersApiClient _users = users;
    private readonly PlacesApiClient _places = places;

    public async Task<ApiResult<IReadOnlyList<LookupItemVm>>> SuggestUsersAsync(string q, CancellationToken ct)
    {
        var result = await _users.SuggestAsync(q, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<IReadOnlyList<LookupItemVm>>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<IReadOnlyList<LookupItemVm>>.Fail(result.StatusCode, result.Error ?? "Could not load user suggestions.");
        }

        IReadOnlyList<LookupItemVm> items = result.Data
            .Select(u => new LookupItemVm(u.Id, u.Email))
            .ToList();
        return ApiResult<IReadOnlyList<LookupItemVm>>.Ok(items);
    }

    public async Task<ApiResult<IReadOnlyList<LookupItemVm>>> SuggestPlacesAsync(string q, CancellationToken ct)
    {
        var result = await _places.SuggestAsync(q, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<IReadOnlyList<LookupItemVm>>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<IReadOnlyList<LookupItemVm>>.Fail(result.StatusCode, result.Error ?? "Could not load place suggestions.");
        }

        IReadOnlyList<LookupItemVm> items = result.Data
            .Select(p => new LookupItemVm(p.Id, p.Name))
            .ToList();
        return ApiResult<IReadOnlyList<LookupItemVm>>.Ok(items);
    }

    /// <summary>
    /// No-JS fallback (PE1): resolves a typed user query to an id — exact email match
    /// first, otherwise a single unambiguous suggestion. Null when nothing resolves.
    /// </summary>
    public async Task<Guid?> ResolveUserIdAsync(string query, CancellationToken ct)
    {
        var result = await _users.SuggestAsync(query.Trim(), ct);
        if (result is not { IsSuccess: true, Data: not null } || result.Data.Count == 0)
        {
            return null;
        }

        var exact = result.Data.FirstOrDefault(u => string.Equals(u.Email, query.Trim(), StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            return exact.Id;
        }

        return result.Data.Count == 1 ? result.Data[0].Id : null;
    }

    /// <summary>
    /// No-JS fallback (PE1): resolves a typed place name to an id — exact name match
    /// first, otherwise a single unambiguous suggestion. Null when nothing resolves.
    /// </summary>
    public async Task<Guid?> ResolvePlaceIdAsync(string query, CancellationToken ct)
    {
        var result = await _places.SuggestAsync(query.Trim(), ct);
        if (result is not { IsSuccess: true, Data: not null } || result.Data.Count == 0)
        {
            return null;
        }

        var exact = result.Data.FirstOrDefault(p => string.Equals(p.Name, query.Trim(), StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            return exact.Id;
        }

        return result.Data.Count == 1 ? result.Data[0].Id : null;
    }
}
