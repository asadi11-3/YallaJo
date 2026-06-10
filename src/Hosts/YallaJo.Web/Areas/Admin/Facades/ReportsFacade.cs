using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Reports;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class ReportsFacade
{
    private const int DefaultPageSize = 20;
    private readonly ReportsApiClient _api;
    private readonly UsersApiClient _users;

    public ReportsFacade(ReportsApiClient api, UsersApiClient users)
    {
        _api = api;
        _users = users;
    }

    public async Task<ApiResult<ReportsVm>> GetIndexAsync(Guid? afterCursor, int pageSize, CancellationToken ct)
    {
        if (pageSize < 1 || pageSize > 50)
        {
            pageSize = DefaultPageSize;
        }

        var result = await _api.GetAdminReportsAsync(afterCursor, pageSize, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<ReportsVm>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<ReportsVm>.Fail(result.StatusCode, result.Error ?? "Could not load reports.");
        }

        var reporterEmails = await ResolveUserEmailsAsync(result.Data.Items.Select(r => r.ReporterUserId), ct);
        return ApiResult<ReportsVm>.Ok(ReportsMapper.ToVm(result.Data, reporterEmails));
    }

    private async Task<IReadOnlyDictionary<Guid, string>> ResolveUserEmailsAsync(IEnumerable<Guid> userIds, CancellationToken ct)
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
            if (pair is { } kvp)
            {
                map[kvp.Key] = kvp.Value;
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
            // Tolerant: leave unresolved so the view shows the localized fallback (F10).
        }

        return null;
    }

    public Task<ApiResult> ResolveAsync(Guid id, string action, string? notes, CancellationToken ct)
        => Normalize(_api.ResolveAsync(id, action, notes, ct), "Could not resolve the report.");

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
            return ApiResult.Fail(404, "Report not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "This action is not allowed in the report's current state. Please reload and try again.");
        }

        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
