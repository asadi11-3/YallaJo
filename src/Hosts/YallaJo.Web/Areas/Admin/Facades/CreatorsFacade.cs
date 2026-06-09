using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Creators;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class CreatorsFacade
{
    private readonly CreatorsApiClient _api;
    private readonly UsersApiClient _users;

    public CreatorsFacade(CreatorsApiClient api, UsersApiClient users)
    {
        _api = api;
        _users = users;
    }

    public async Task<ApiResult<CreatorsVm>> GetIndexAsync(
        string? status, Guid? id, int page, CancellationToken ct)
    {
        var pageSize = 20;
        var list = await _api.GetApplicationsAsync(status, page, pageSize, ct);
        if (list.IsUnauthorized)
        {
            return ApiResult<CreatorsVm>.ForceSignOut();
        }

        if (list is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<CreatorsVm>.Fail(list.StatusCode, list.Error ?? "Could not load creator applications.");
        }

        CreatorApplicationDetailResponse? detail = null;
        if (id is { } applicationId && applicationId != Guid.Empty)
        {
            var d = await _api.GetApplicationAsync(applicationId, ct);
            if (d.IsUnauthorized)
            {
                return ApiResult<CreatorsVm>.ForceSignOut();
            }

            if (d is { IsSuccess: true, Data: not null })
            {
                detail = d.Data;
            }
        }

        // F10: resolve applicant GUIDs to human-readable emails so the view never shows a raw GUID (API1: concurrent, R7: no N+1).
        var applicantIds = list.Data.Items.Select(i => i.ApplicantUserId);
        if (detail is not null)
        {
            applicantIds = applicantIds.Append(detail.ApplicantUserId);
        }

        var applicantEmails = await ResolveApplicantEmailsAsync(applicantIds, ct);

        return ApiResult<CreatorsVm>.Ok(CreatorsMapper.ToVm(list.Data, detail, status, applicantEmails));
    }

    private async Task<IReadOnlyDictionary<Guid, string>> ResolveApplicantEmailsAsync(
        IEnumerable<Guid> userIds, CancellationToken ct)
    {
        var distinct = userIds.Where(g => g != Guid.Empty).Distinct().ToList();
        if (distinct.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var tasks = distinct.Select(uid => ResolveOneAsync(uid, ct)).ToList();
        var pairs = await Task.WhenAll(tasks);

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
            // Graceful degradation: leave the applicant unresolved -> view shows a localized fallback, never a raw GUID.
        }

        return null;
    }

    public Task<ApiResult> ApproveAsync(Guid id, string displayName, string? avatarUrl, CancellationToken ct)
        => Normalize(_api.ApproveAsync(id, displayName, avatarUrl, ct), "Could not approve the creator application.");

    public Task<ApiResult> RejectAsync(Guid id, string reason, CancellationToken ct)
        => Normalize(_api.RejectAsync(id, reason, ct), "Could not reject the creator application.");

    public Task<ApiResult> RequestMoreInfoAsync(Guid id, string adminNote, CancellationToken ct)
        => Normalize(_api.RequestMoreInfoAsync(id, adminNote, ct), "Could not request more information.");

    public Task<ApiResult> SuspendAsync(Guid profileId, string reason, CancellationToken ct)
        => Normalize(_api.SuspendAsync(profileId, reason, ct), "Could not suspend the creator profile.");

    public Task<ApiResult> ReinstateAsync(Guid profileId, CancellationToken ct)
        => Normalize(_api.ReinstateAsync(profileId, ct), "Could not reinstate the creator profile.");

    // ── §8.6: tier, edit, delete, invitation ───────────────────────────────────

    public Task<ApiResult> PromoteAsync(Guid profileId, string? targetTier, CancellationToken ct)
        => string.IsNullOrWhiteSpace(targetTier)
            ? Task.FromResult(ApiResult.Fail(400, "A target tier is required."))
            : Normalize(_api.PromoteAsync(profileId, targetTier.Trim(), ct), "Could not promote the creator.");

    public Task<ApiResult> DemoteAsync(Guid profileId, string? targetTier, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(targetTier))
        {
            return Task.FromResult(ApiResult.Fail(400, "A target tier is required."));
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Task.FromResult(ApiResult.Fail(400, "A demotion reason is required."));
        }
        return Normalize(_api.DemoteAsync(profileId, targetTier.Trim(), reason.Trim(), ct), "Could not demote the creator.");
    }

    public Task<ApiResult> EditAsync(Guid id, string? displayName, string? bio, string? avatarUrl, string? slug, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Task.FromResult(ApiResult.Fail(400, "A display name is required."));
        }

        var body = new
        {
            displayName = displayName.Trim(),
            bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim(),
            avatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim(),
            slug = string.IsNullOrWhiteSpace(slug) ? null : slug.Trim(),
        };
        return Normalize(_api.EditAsync(id, body, ct), "Could not update the creator profile.");
    }

    public Task<ApiResult> DeleteAsync(Guid id, string? reason, CancellationToken ct)
        => string.IsNullOrWhiteSpace(reason)
            ? Task.FromResult(ApiResult.Fail(400, "A reason is required to delete a creator profile."))
            : Normalize(_api.DeleteAsync(id, reason.Trim(), ct), "Could not delete the creator profile.");

    public Task<ApiResult> SendInvitationAsync(string? kind, string? email, Guid? invitedUserId, string? personalMessage, CancellationToken ct)
    {
        var normalizedKind = string.IsNullOrWhiteSpace(kind) ? "Email" : kind.Trim();

        if (string.Equals(normalizedKind, "Email", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(email))
        {
            return Task.FromResult(ApiResult.Fail(400, "An email address is required for an email invitation."));
        }
        if (string.Equals(normalizedKind, "InApp", StringComparison.OrdinalIgnoreCase) && (invitedUserId is null || invitedUserId == Guid.Empty))
        {
            return Task.FromResult(ApiResult.Fail(400, "A user is required for an in-app invitation."));
        }

        var body = new
        {
            kind = normalizedKind,
            email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            invitedUserId,
            personalMessage = string.IsNullOrWhiteSpace(personalMessage) ? null : personalMessage.Trim(),
        };
        return Normalize(_api.SendInvitationAsync(body, ct), "Could not send the creator invitation.");
    }

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
            return ApiResult.Fail(404, "Creator application or profile not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "This action is not allowed in the current state. Please reload and try again.");
        }

        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
