using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.GuideApplications;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class GuideApplicationsFacade
{
    private const int PageSize = 20;
    private const int TourLookupPageSize = 200;

    private readonly GuideApplicationsApiClient _api;
    private readonly AdminToursApiClient _tours;
    private readonly UsersApiClient _users;

    public GuideApplicationsFacade(
        GuideApplicationsApiClient api,
        AdminToursApiClient tours,
        UsersApiClient users)
    {
        _api = api;
        _tours = tours;
        _users = users;
    }

    public async Task<ApiResult<GuideApplicationsVm>> GetIndexAsync(
        Guid? tourId,
        string? status,
        int page,
        CancellationToken ct)
    {
        // Tour-name options always load so the F10 tour picker is available even on the no-context view.
        var tourOptions = await LoadTourOptionsAsync(ct);

        if (tourId is not { } id || id == Guid.Empty)
        {
            return ApiResult<GuideApplicationsVm>.Ok(new GuideApplicationsVm
            {
                StatusFilter = status,
                Page = page,
                PageSize = PageSize,
                TourOptions = tourOptions,
            });
        }

        var result = await _api.GetApplicationsAsync(id, status, page, PageSize, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<GuideApplicationsVm>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<GuideApplicationsVm>.Fail(
                result.StatusCode,
                result.Error ?? "Could not load guide applications.");
        }

        // Resolve applying-guide identities (F10) concurrently (API1).
        var guideEmails = await ResolveGuideEmailsAsync(result.Data.Items.Select(i => i.GuideUserId), ct);

        var vm = GuideApplicationsMapper.ToVm(result.Data, id, status, page, PageSize, guideEmails);
        vm.TourOptions = tourOptions;
        return ApiResult<GuideApplicationsVm>.Ok(vm);
    }

    private async Task<IReadOnlyList<GuideTourOptionVm>> LoadTourOptionsAsync(CancellationToken ct)
    {
        try
        {
            var result = await _tours.ListAsync(status: null, page: 1, pageSize: TourLookupPageSize, sort: null, ct);
            if (result is not { IsSuccess: true, Data: not null })
            {
                return [];
            }

            return result.Data.Items
                .Select(t => new GuideTourOptionVm { Id = t.Id, Name = t.Name })
                .OrderBy(o => o.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private async Task<IReadOnlyDictionary<Guid, string>> ResolveGuideEmailsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken ct)
    {
        var distinct = userIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (distinct.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var pairs = await Task.WhenAll(distinct.Select(id => ResolveOneAsync(id, ct)));
        var map = new Dictionary<Guid, string>();
        foreach (var pair in pairs)
        {
            if (pair is { } kv)
            {
                map[kv.Key] = kv.Value;
            }
        }

        return map;
    }

    private async Task<KeyValuePair<Guid, string>?> ResolveOneAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            var result = await _users.GetUserAsync(userId, ct);
            if (result is { IsSuccess: true, Data: not null } && !string.IsNullOrWhiteSpace(result.Data.Email))
            {
                return new KeyValuePair<Guid, string>(userId, result.Data.Email);
            }
        }
        catch
        {
            // Graceful F10 fallback: leave the name unresolved.
        }

        return null;
    }

    public Task<ApiResult> ApproveAsync(Guid tourId, Guid applicationId, CancellationToken ct) =>
        Normalize(_api.ApproveAsync(tourId, applicationId, ct), "Could not approve the guide application.");

    public Task<ApiResult> RejectAsync(Guid tourId, Guid applicationId, string reason, CancellationToken ct) =>
        Normalize(_api.RejectAsync(tourId, applicationId, reason, ct), "Could not reject the guide application.");

    private static async Task<ApiResult> Normalize(Task<ApiResult> call, string fallback)
    {
        var result = await call;
        if (result.IsSuccess)
        {
            return ApiResult.Ok();
        }

        if (result.IsUnauthorized)
        {
            return ApiResult.ForceSignOut();
        }

        if (result.IsNotFound)
        {
            return ApiResult.Fail(404, "Guide application not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "This action is not allowed in the application's current state. Please reload and try again.");
        }

        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
