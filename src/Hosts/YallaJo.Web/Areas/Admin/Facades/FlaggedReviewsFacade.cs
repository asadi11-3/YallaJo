using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.FlaggedReviews;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class FlaggedReviewsFacade
{
    private const int DefaultPageSize = 20;
    private readonly FlaggedReviewsApiClient _api;
    private readonly UsersApiClient _users;

    public FlaggedReviewsFacade(FlaggedReviewsApiClient api, UsersApiClient users)
    {
        _api = api;
        _users = users;
    }

    public async Task<ApiResult<FlaggedReviewsVm>> GetIndexAsync(Guid? afterCursor, int pageSize, CancellationToken ct)
    {
        if (pageSize < 1 || pageSize > 50)
        {
            pageSize = DefaultPageSize;
        }

        var result = await _api.GetFlaggedAsync(afterCursor, pageSize, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<FlaggedReviewsVm>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<FlaggedReviewsVm>.Fail(result.StatusCode, result.Error ?? "Could not load flagged reviews.");
        }

        var reviewerEmails = await ResolveUserEmailsAsync(result.Data.Items.Select(r => r.UserId), ct);
        return ApiResult<FlaggedReviewsVm>.Ok(FlaggedReviewsMapper.ToVm(result.Data, reviewerEmails));
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
            // graceful F10 fallback: leave name unresolved
        }

        return null;
    }

    public Task<ApiResult> ApproveAsync(Guid id, string? rowVersion, string? notes, CancellationToken ct)
        => Normalize(_api.ApproveAsync(id, rowVersion, notes, ct), "Could not approve the review.");

    public Task<ApiResult> RemoveAsync(Guid id, string? rowVersion, string? notes, CancellationToken ct)
        => Normalize(_api.RemoveAsync(id, rowVersion, notes, ct), "Could not remove the review.");

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
            return ApiResult.Fail(404, "Review not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "This review was modified by someone else. Please reload and try again.");
        }

        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
