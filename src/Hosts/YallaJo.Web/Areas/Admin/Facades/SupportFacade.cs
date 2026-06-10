using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Support;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class SupportFacade
{
    private const int DefaultPageSize = 20;
    private const int AdminOptionsPageSize = 200;
    private readonly SupportApiClient _api;
    private readonly UsersApiClient _users;

    public SupportFacade(SupportApiClient api, UsersApiClient users)
    {
        _api = api;
        _users = users;
    }

    public async Task<ApiResult<SupportListVm>> GetIndexAsync(
        string? status, string? category, Guid? cursor, int pageSize, CancellationToken ct)
    {
        if (pageSize < 1 || pageSize > 50)
        {
            pageSize = DefaultPageSize;
        }

        var result = await _api.GetTicketsAsync(status, category, cursor, pageSize, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<SupportListVm>.ForceSignOut();
        }
        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<SupportListVm>.Fail(result.StatusCode, result.Error ?? "Could not load support tickets.");
        }

        var emails = await ResolveUserEmailsAsync(
            result.Data.Items
                .SelectMany(t => new[] { t.CreatedByUserId, t.AssignedToUserId ?? Guid.Empty }), ct);
        return ApiResult<SupportListVm>.Ok(SupportMapper.ToListVm(result.Data, status, category, emails));
    }

    public async Task<ApiResult<SupportTicketDetailVm>> GetDetailsAsync(Guid id, CancellationToken ct)
    {
        var result = await _api.GetTicketAsync(id, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<SupportTicketDetailVm>.ForceSignOut();
        }
        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<SupportTicketDetailVm>.Fail(result.StatusCode, result.Error ?? "Could not load the support ticket.");
        }

        var ticket = result.Data;
        var userIds = new List<Guid> { ticket.CreatedByUserId };
        if (ticket.AssignedToUserId is { } aid) userIds.Add(aid);
        if (ticket.ResolvedByUserId is { } rid) userIds.Add(rid);
        if (ticket.Messages is not null) userIds.AddRange(ticket.Messages.Select(m => m.AuthorUserId));

        var emailsTask = ResolveUserEmailsAsync(userIds, ct);
        var optionsTask = LoadAdminOptionsAsync(ct);
        await Task.WhenAll(emailsTask, optionsTask);

        return ApiResult<SupportTicketDetailVm>.Ok(
            SupportMapper.ToDetailVm(ticket, await emailsTask, await optionsTask));
    }

    private async Task<IReadOnlyList<SupportAdminOptionVm>> LoadAdminOptionsAsync(CancellationToken ct)
    {
        try
        {
            var result = await _users.GetUsersAsync(1, AdminOptionsPageSize, ct);
            if (result is { IsSuccess: true, Data: not null })
            {
                return result.Data.Items
                    .Where(u => !string.IsNullOrWhiteSpace(u.Email))
                    .Select(u => new SupportAdminOptionVm { Id = u.Id, Email = u.Email })
                    .OrderBy(o => o.Email, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            }
        }
        catch
        {
            // Tolerant: empty options simply hide the assign dropdown choices.
        }

        return [];
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

    public Task<ApiResult> PostMessageAsync(Guid id, string body, bool isInternal, CancellationToken ct)
        => Normalize(_api.PostMessageAsync(id, body, isInternal, ct), "Could not post the reply.");

    public Task<ApiResult> CloseAsync(Guid id, string? rowVersion, CancellationToken ct)
        => Normalize(_api.CloseAsync(id, rowVersion, ct), "Could not close the ticket.");

    public Task<ApiResult> AssignAsync(Guid id, Guid adminUserId, CancellationToken ct)
        => Normalize(_api.AssignAsync(id, adminUserId, ct), "Could not assign the ticket.");

    public Task<ApiResult> ResolveAsync(Guid id, string? notes, string? rowVersion, CancellationToken ct)
        => Normalize(_api.ResolveAsync(id, notes, rowVersion, ct), "Could not resolve the ticket.");

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
            return ApiResult.Fail(404, "Support ticket not found.");
        }
        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "This ticket was modified by someone else. Please reload and try again.");
        }
        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
